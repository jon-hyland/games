using Common.Standard.Configuration;
using Common.Standard.Error;
using Common.Standard.Extensions;
using Common.Standard.Game;
using Common.Standard.Logging;
using Common.Standard.Networking.Packets;
using Common.Standard.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Common.Standard.Networking
{
    /// <summary>
    /// </summary>
    public class GameCommunications : IDisposable
    {
        //const
        private const int INVITE_TIMEOUT_SEC = 20;

        //private
        private readonly IGameConfig _config = null;
        private readonly Player _localPlayer = null;
        private readonly Client _client = null;
        private readonly CommandManager _commandManager = new CommandManager();
        private readonly SimpleTimer _maintenanceTimer = null;
        private readonly List<PacketBase> _incomingPackets = new List<PacketBase>();
        private readonly ManualResetEventSlim _incomingPacketSignal = new ManualResetEventSlim();
        private readonly Thread _incomingPacketThread = null;
        private readonly Thread _heartbeatThread = null;
        private readonly object _inviteLock = new object();
        private ushort _commandSequence = 0;
        private ConnectionState _connectionState = ConnectionState.NotConnected;
        private long _heartbeatsSent = 0;
        private long _dataSent = 0;
        private long _dataReceived = 0;
        private long _commandRequestsSent = 0;
        private long _commandRequestsReceived = 0;
        private long _commandResponsesSent = 0;
        private long _commandResponsesReceived = 0;
        private bool _isStarted = false;
        private bool _isStopped = false;
        private int _tcpErrors = 0;

        //public
        public string GameTitle => _config.GameTitle;
        public Version GameVersion => _config.GameVersion;
        public IPAddress LocalIP => _config.LocalIP;
        public Player LocalPlayer => _localPlayer;
        public ConnectionState ConnectionState => _connectionState;
        public long HeartbeatsSent => _heartbeatsSent;
        public long DataSent => _dataSent;
        public long DataReceived => _dataReceived;
        public long CommandRequestsSent => _commandRequestsSent;
        public long CommandRequestsReceived => _commandRequestsReceived;
        public long CommandResponsesSent => _commandResponsesSent;
        public long CommandResponsesReceived => _commandResponsesReceived;

        //events
        public event Action<Player> OpponentConnected;
        public event Action<Player> OpponentInviteReceived;
        public event Action SessionEnded;
        public event Action<CommandRequestPacket> CommandRequestPacketReceived;
        public event Action<CommandResponsePacket> CommandResponsePacketReceived;
        public event Action<DataPacket> DataPacketReceived;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public GameCommunications(IGameConfig config, string playerName)
        {
            try
            {
                //vars
                _config = config;
                _localPlayer = new Player(config.LocalIP, config.GameTitle, config.GameVersion, playerName);
                _client = new Client(config.ServerIP, config.ServerPort);
                _maintenanceTimer = new SimpleTimer(MaintenanceTimer_Callback, 15, false);
                _incomingPacketThread = new Thread(IncomingPacket_Thread);
                _incomingPacketThread.IsBackground = true;
                _heartbeatThread = new Thread(Heartbeat_Thread);
                _heartbeatThread.IsBackground = true;

                //events
                _client.PacketReceived += (c, p) => PacketReceived(p);
            }
            catch (Exception ex)
            {
                _connectionState = ConnectionState.Disabled;
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            _isStopped = true;
            _client?.Dispose();
            _commandManager?.Dispose();
            _maintenanceTimer?.Dispose();
        }

        #region Start / Stop

        /// <summary>
        /// Starts game communications with server, heartbeat and processing threads, etc.
        /// Returns false if unable to establish connection or other error.
        /// </summary>
        public bool Start()
        {
            try
            {
                //return if disabled
                if (_connectionState == ConnectionState.Disabled)
                    return false;

                //prevent restart
                if (_isStarted)
                    throw new Exception("Cannot restart communications");
                _isStarted = true;

                //open client connection to server
                if (!_client.Connect(timeoutMs: 2500))
                    throw new Exception("Unable to connect to game server");
                _connectionState = ConnectionState.Connected;

                //start incoming packet thread
                _incomingPacketThread.Start();

                //start maintenance timer
                _maintenanceTimer.Start();

                //start heartbeat thread
                _heartbeatThread.Start();

                //success
                return true;
            }
            catch (Exception ex)
            {
                _connectionState = ConnectionState.Disabled;
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        #endregion

        #region Player Discovery

        /// <summary>
        /// Changes the broadcasted player's name.
        /// </summary>
        public void ChangePlayerName(string name)
        {
            _localPlayer.Name = name;
        }

        /// <summary>
        /// Returns list of most recent discovered players, for this game version.
        /// </summary>
        public IReadOnlyList<Player> GetPlayers(int top = 5)
        {
            try
            {
                CommandResult result = SendCommandRequest(_config.ServerIP, CommandType.GetPlayers, null, TimeSpan.FromMilliseconds(750));
                if ((result.Code == ResultCode.Accept) && (result.ResponsePacket != null))
                {
                    List<Player> players = new List<Player>();
                    PacketParser parser = new PacketParser(result.ResponsePacket.Data);
                    ushort count = parser.GetUInt16();
                    for (int i = 0; i < count; i++)
                    {
                        byte[] bytes = parser.GetBytes();
                        Player player = Player.FromBytes(bytes);
                        if (player != null)
                        {
                            players.Add(player);
                            if (players.Count >= top)
                                break;
                        }
                    }
                    return players;
                }
            }
            catch (Exception ex)
            {
                Log.Write("GetPlayers: Error fetching player list from server");
                ErrorHandler.LogError(ex);
            }
            return new List<Player>();
        }

        #endregion

        #region Opponent Invites

        /// <summary>
        /// Sends invite request to opponent, waits for response or timeout.
        /// </summary>
        public CommandResult InviteOpponent(Player opponent)
        {
            CommandResult result = new CommandResult(ResultCode.Unspecified);
            try
            {
                lock (_inviteLock)
                {
                    //send connect-request command
                    byte[] data = PacketBuilder.ToBytes(new object[] { _localPlayer.Name });
                    result = SendCommandRequest(opponent.IP, CommandType.ConnectToPlayer, data, TimeSpan.FromSeconds(INVITE_TIMEOUT_SEC));
                }

                //fire event?
                if (result.Code == ResultCode.Accept)
                    OpponentConnected?.InvokeFromTask(opponent);
            }
            catch (Exception ex)
            {
                Log.Write("InviteOpponent: Unknown error");
                ErrorHandler.LogError(ex);
                result.Code = ResultCode.Error;
            }
            return result;
        }

        /// <summary>
        /// Accepts an invite from a remote opponent.
        /// </summary>
        public bool AcceptInvite(Player opponent)
        {
            try
            {
                lock (_inviteLock)
                {
                    //send acceptance
                    return SendCommandResponse(
                        destinationIP: opponent.IP,
                        type: CommandType.ConnectToPlayer,
                        sequence: opponent.InviteSequence,
                        code: ResultCode.Accept,
                        data: null);
                }
            }
            catch (Exception ex)
            {
                Log.Write("AcceptInvite: Unknown error");
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// Sends rejection response to server.
        /// </summary>
        public bool RejectInvite(Player opponent)
        {
            try
            {
                lock (_inviteLock)
                {
                    //send rejection
                    return SendCommandResponse(
                        destinationIP: opponent.IP,
                        type: CommandType.ConnectToPlayer,
                        sequence: opponent.InviteSequence,
                        code: ResultCode.Reject,
                        data: null);
                }
            }
            catch (Exception ex)
            {
                Log.Write("RejectInvite: Unknown error");
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        #endregion

        #region Outgoing

        /// <summary>
        /// Sends command request, blocks until response or timeout.  Data is optional.
        /// </summary>
        public CommandResult SendCommandRequest(IPAddress destinationIP, CommandType type, byte[] data, TimeSpan timeout)
        {
            try
            {
                //disabled?
                if (_connectionState == ConnectionState.Disabled)
                    return new CommandResult(ResultCode.Error);

                //vars
                ushort sequence;
                lock (this)
                {
                    _commandSequence++;
                    if (_commandSequence >= UInt16.MaxValue)
                        _commandSequence = 1;
                    sequence = _commandSequence;
                }

                //create packet
                CommandRequestPacket packet = new CommandRequestPacket(
                    gameTitle: _config.GameTitle,
                    gameVersion: _config.GameVersion,
                    sourceIP: _config.LocalIP,
                    destinationIP: destinationIP,
                    playerName: _localPlayer.Name,
                    commandType: type,
                    sequence: sequence,
                    retryAttempt: 0,
                    timeoutMs: (uint)timeout.TotalMilliseconds,
                    data: data);

                //send packet
                _client.SendPacket(packet);
                _commandRequestsSent++;

                //record command request has been sent
                _commandManager.RequestSent(packet);

                //loop
                while (true)
                {
                    //get current status
                    CommandResult result = _commandManager.GetCommandStatus(sequence);

                    //have answer or timeout?  return!
                    if (result.Code != ResultCode.Unspecified)
                        return result;

                    //sleep
                    Thread.Sleep(15);
                }
            }
            catch (Exception ex)
            {
                _tcpErrors++;
                Log.Write("SendCommandRequest: Error sending data");
                ErrorHandler.LogError(ex);
                return new CommandResult(ResultCode.Error);
            }
        }

        /// <summary>
        /// Sends command response.  Data is optional.
        /// </summary>
        public bool SendCommandResponse(IPAddress destinationIP, CommandType type, ushort sequence, ResultCode code, byte[] data = null)
        {
            try
            {
                //disabled?
                if (_connectionState == ConnectionState.Disabled)
                    return false;

                //create packet
                CommandResponsePacket packet = new CommandResponsePacket(
                    gameTitle: _config.GameTitle,
                    gameVersion: _config.GameVersion,
                    sourceIP: _config.LocalIP,
                    destinationIP: destinationIP,
                    playerName: _localPlayer.Name,
                    commandType: type,
                    sequence: sequence,
                    code: code,
                    data: data);

                //send packet
                _client.SendPacket(packet);
                _commandResponsesSent++;

                //success
                return true;
            }
            catch (Exception ex)
            {
                _tcpErrors++;
                Log.Write("SendCommandResponse: Error sending data");
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// Sends generic one-way data packet.
        /// </summary>
        public bool SendData(IPAddress destinationIP, byte[] data)
        {
            try
            {
                //disabled?
                if (_connectionState == ConnectionState.Disabled)
                    return false;

                //create packet
                DataPacket packet = new DataPacket(
                    gameTitle: _config.GameTitle,
                    gameVersion: _config.GameVersion,
                    sourceIP: _config.LocalIP,
                    destinationIP: destinationIP,
                    playerName: _localPlayer.Name,
                    data: data);

                //send packet
                _client.SendPacket(packet);
                _dataSent++;

                //success
                return true;
            }
            catch (Exception ex)
            {
                _tcpErrors++;
                Log.Write("SendData: Error sending data");
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// Sends heartbeat packet to server.
        /// </summary>
        public bool SendHeartbeat()
        {
            try
            {
                //create packet
                HeartbeatPacket packet = new HeartbeatPacket(
                    gameTitle: _config.GameTitle,
                    gameVersion: _config.GameVersion,
                    sourceIP: _config.LocalIP,
                    destinationIP: _config.ServerIP,
                    playerName: _localPlayer.Name,
                    count: _heartbeatsSent + 1);

                //send packet
                _client.SendPacket(packet);
                _heartbeatsSent++;

                //success
                return true;
            }
            catch (Exception ex)
            {
                _tcpErrors++;
                Log.Write("SendHeartbeat: Error sending data");
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        #endregion

        #region Incomming

        /// <summary>
        /// Fired when packet received.
        /// </summary>
        private void PacketReceived(PacketBase packet)
        {
            try
            {
                //reject if wrong game or version
                if ((packet.GameTitle != _config.GameTitle) || (packet.GameVersion != _config.GameVersion))
                    return;

                //special logic for invite requests
                if ((packet is CommandRequestPacket p2) && (p2.CommandType == CommandType.ConnectToPlayer))
                {
                    _commandRequestsReceived++;
                    PacketParser parser = new PacketParser(p2.Data);
                    string playerName = parser.GetString();
                    Player pendingOpponent = new Player(p2.SourceIP, p2.GameTitle, p2.GameVersion, playerName, p2.Sequence);
                    OpponentInviteReceived?.InvokeFromTask(pendingOpponent);
                    return;
                }

                ////special logic for end-session requests
                //if ((packet is CommandRequestPacket p3) && (p3.CommandType == CommandType.))

                ////reject if wrong opponent
                //if ((!packet.SourceIP.Equals(_config.ServerIP)) && ((_opponent == null) || (!packet.SourceIP.Equals(_opponent.IP))))
                //    return;

                //queue packet for processing
                lock (_incomingPackets)
                {
                    _incomingPackets.Add(packet);
                }

                //signal
                _incomingPacketSignal.Set();
            }
            catch (Exception ex)
            {
                _tcpErrors++;
                Log.Write("PacketReceived: Error processing packet");
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Thread to process incoming packets.
        /// </summary>
        private void IncomingPacket_Thread()
        {
            while (true)
            {
                try
                {
                    //exit if stopped
                    if (_isStopped)
                        return;

                    //wait for data signal, or 15ms
                    _incomingPacketSignal.Wait(15);

                    //vars
                    PacketBase[] packets = null;

                    //lock queue
                    lock (_incomingPackets)
                    {
                        //get packets, if exist
                        if (_incomingPackets.Count > 0)
                        {
                            packets = _incomingPackets.ToArray();
                            _incomingPackets.Clear();
                            _incomingPacketSignal.Reset();
                        }
                    }
                    if (packets == null)
                        continue;

                    //process packets
                    foreach (PacketBase packet in packets)
                    {
                        //command request packet
                        if (packet is CommandRequestPacket p1)
                        {
                            //increment counter
                            _commandRequestsReceived++;

                            //fire event
                            CommandRequestPacketReceived?.InvokeFromTask(p1);
                        }

                        //command response packet
                        else if (packet is CommandResponsePacket p2)
                        {
                            //increment counter
                            _commandResponsesReceived++;

                            //record command response received
                            _commandManager.ResponseReceived(p2);

                            //fire event
                            CommandResponsePacketReceived?.InvokeFromTask(p2);
                        }

                        //data packet
                        else if (packet is DataPacket p3)
                        {
                            //increment counter
                            _dataReceived++;

                            //fire event
                            DataPacketReceived?.InvokeFromTask(p3);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write("IncomingPacket_Thread: Error processing packet");
                    ErrorHandler.LogError(ex);
                }
            }
        }

        #endregion

        #region Maintenance

        /// <summary>
        /// Fired by timer, performs maintenance.
        /// </summary>
        private void MaintenanceTimer_Callback()
        {
            try
            {
                //connection broken?
                if ((_connectionState == ConnectionState.Connected) && (!_client.IsConnected))
                {
                    Log.Write("Disconnect: Connection broken");
                    _connectionState = ConnectionState.NotConnected;
                    //Disconnected?.InvokeFromTask();
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Write("MaintenanceTimer_Callback: Unknown error");
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Heartbeat thread, sends heartbeat packets every ~100ms.
        /// </summary>
        private void Heartbeat_Thread()
        {
            const int intervalMs = 250;
            DateTime lastSend = DateTime.MinValue;
            while (true)
            {
                try
                {
                    //exit if stopped
                    if (_isStopped)
                        return;

                    //sleep
                    TimeSpan elapsed = DateTime.Now - lastSend;
                    int sleepMs = Math.Max(intervalMs - (int)elapsed.TotalMilliseconds, 0);
                    Thread.Sleep(sleepMs);

                    //send heartbeat to opponent
                    lastSend = DateTime.Now;
                    SendHeartbeat();
                }
                catch (Exception ex)
                {
                    Log.Write("Heartbeat_Thread: Unknown error");
                    ErrorHandler.LogError(ex);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents the system's connection states.
    /// Mostly refers to this instance's client object, not it's server object
    /// which is always open for connections.
    /// </summary>
    public enum ConnectionState
    {
        Disabled,
        NotConnected,
        Connected
    }
}

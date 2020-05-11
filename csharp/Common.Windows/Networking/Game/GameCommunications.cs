using Common.Standard.Error;
using Common.Standard.Extensions;
using Common.Standard.Game;
using Common.Standard.Logging;
using Common.Standard.Networking;
using Common.Standard.Networking.Packets;
using Common.Standard.Threading;
using Common.Windows.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Common.Windows.Networking.Game
{
    /// <summary>
    /// Simple function SetPlayer.. disconnects if connected.
    /// Simple function SendCommand.. reconnects if not connected (no matter if accepted).
    /// Move connect logic outside class?  Simplify logic?
    /// Add event for connection request, function for user connection response.
    /// Both places set a connection flag.  Change from ConnectionState?
    /// </summary>
    public class GameCommunications : IDisposable
    {
        //const
        private const int INVITE_TIMEOUT_SEC = 20;

        //private
        private readonly IConfig _config;
        private readonly Player _localPlayer;
        //private readonly DiscoveryClient _discoveryClient;
        //private readonly DiscoveryServer _discoveryServer;
        //private readonly DiscoveredPlayers _discoveredPlayers;
        //private readonly SimpleTcpClient _dataClient;
        //private readonly SimpleTcpServer _dataServer;
        private readonly Client _client;
        //private readonly List<byte> _incomingBuffer;
        private readonly CommandManager _commandManager;
        private readonly SimpleTimer _maintenanceTimer;
        private readonly List<PacketBase> _incomingPackets;
        private readonly ManualResetEventSlim _incomingPacketSignal;
        private readonly Thread _incomingPacketThread;
        private readonly Thread _heartbeatThread;
        private readonly object _inviteLock = new object();
        private ushort _commandSequence;
        //private ConnectionState _connectionState;
        private Player _opponent;
        private Player _pendingOpponent;
        private long _heartbeatsSent;
        private long _heartbeatsReceived;
        private long _dataSent;
        private long _dataReceived;
        private long _commandRequestsSent;
        private long _commandRequestsReceived;
        private long _commandResponsesSent;
        private long _commandResponsesReceived;
        //private DateTime _lastHeartbeatReceived => DateTime.Now;    //fix
        private bool _isStarted;
        private bool _isStopped;
        private bool _isDisabled;


        //public
        public string GameTitle => _config.GameTitle;
        public Version GameVersion => _config.GameVersion;
        public IPAddress LocalIP => _config.LocalIP;
        public Player LocalPlayer => _localPlayer;
        public Player Opponent => _opponent;
        public ConnectionState ConnectionState => ConnectionState.Connected;    //fix
        public long HeartbeatsSent => _heartbeatsSent;
        public long HeartbeatsReceived => _heartbeatsReceived;
        public long DataSent => _dataSent;
        public long DataReceived => _dataReceived;
        public long CommandRequestsSent => _commandRequestsSent;
        public long CommandRequestsReceived => _commandRequestsReceived;
        public long CommandResponsesSent => _commandResponsesSent;
        public long CommandResponsesReceived => _commandResponsesReceived;
        //public DateTime LastHeartbeatReceived => _lastHeartbeatReceived;
        //public TimeSpan TimeSinceLastHeartbeatReceived => DateTime.Now - _lastHeartbeatReceived;
        public bool IsDisabled => _isDisabled;

        //events
        public event Action<Player> OpponentConnected;
        public event Action<Player> OpponentInviteReceived;
        public event Action<CommandRequestPacket> CommandRequestPacketReceived;
        public event Action<CommandResponsePacket> CommandResponsePacketReceived;
        public event Action<DataPacket> DataPacketReceived;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public GameCommunications(IConfig config, string playerName)
        {
            try
            {
                //vars
                _config = config;
                _localPlayer = new Player(config.LocalIP, config.GameTitle, config.GameVersion, playerName);
                //_discoveryClient = new DiscoveryClient(config.GameTitle, config.GameVersion, config.LocalIP, config.GamePort, playerName, errorHandler);
                //_discoveryServer = new DiscoveryServer(config.GamePort, errorHandler);
                //_discoveredPlayers = new DiscoveredPlayers();
                //_dataClient = new SimpleTcpClient();
                //_dataServer = new SimpleTcpServer();
                _client = new Client(config.ServerIP, config.ServerPort);
                //_incomingBuffer = new List<byte>();
                _commandManager = new CommandManager();
                _maintenanceTimer = new SimpleTimer(MaintenanceTimer_Callback, 15, false);
                _incomingPackets = new List<PacketBase>();
                _incomingPacketSignal = new ManualResetEventSlim();
                _incomingPacketThread = new Thread(IncomingPacket_Thread)
                {
                    IsBackground = true
                };
                _heartbeatThread = new Thread(Heartbeat_Thread)
                {
                    IsBackground = true
                };
                _commandSequence = 0;
                //_connectionState = ConnectionState.NotConnected;
                _opponent = null;
                _pendingOpponent = null;
                _heartbeatsSent = 0;
                _heartbeatsReceived = 0;
                //_lastHeartbeatReceived = DateTime.MinValue;

                //events
                //_discoveryServer.PlayerAnnounced += (p) => _discoveredPlayers.AddOrUpdatePlayer(p);
                //_dataServer.DataReceived += (s, m) => IncomingData(m?.Data);
                _client.PacketReceived += (c, p) => PacketReceived(p);
            }
            catch (Exception ex)
            {
                _isDisabled = true;
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
                if (_isDisabled)
                    return false;

                //prevent restart
                if (_isStarted)
                    throw new Exception("Cannot restart communications");
                _isStarted = true;

                ////start discovery server
                //_discoveryServer.Start();

                ////start discovery broadcast client
                //_discoveryClient.Start();

                //open client connection to server
                if (!_client.Connect(timeoutMs: 2500))
                    throw new Exception("Unable to connect to game server");

                //start incoming packet thread
                _incomingPacketThread.Start();

                ////start data server
                //_dataServer.Start(_config.GamePort);

                //start maintenance timer
                _maintenanceTimer.Start();

                //start heartbeat thread
                _heartbeatThread.Start();

                //success
                return true;
            }
            catch (Exception ex)
            {
                _isDisabled = true;
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
            //_discoveryClient.PlayerName = name;
            _localPlayer.Name = name;
        }

        /// <summary>
        /// Returns list of most recent discovered players, for this game version.
        /// </summary>
        public IReadOnlyList<Player> GetDiscoveredPlayers(int top = 5)
        {
            CommandResult result = SendCommandRequest(CommandType.GetPlayers, null, TimeSpan.FromMilliseconds(750));
            return new List<Player>();
        }

        #endregion

        #region Opponent Connect and Invites

        /// <summary>
        /// Sets expected opponent player and opens connection, regardless of whether they have accepted invite.  
        /// Allows invite and other communications to be sent.  Set pending to false, if called by receiving side
        /// after *they have* accepted the invite.
        /// </summary>
        public bool SetOpponentAndConnect(Player opponent, bool pending = true)
        {
            try
            {
                bool fireEvent = false;
                try
                {
                    lock (_inviteLock)
                    {
                        ////close any existing connection
                        //_dataClient.Disconnect();
                        //_connectionState = ConnectionState.NotConnected;

                        ////connect to opponent
                        //_dataClient.Connect(opponent.IP.ToString(), _config.GamePort);
                        //_dataClient.TcpClient.SendTimeout = 2000;
                        //_dataClient.TcpClient.ReceiveTimeout = 2000;
                        //_connectionState = pending ? ConnectionState.Connected_PendingInviteAcceptance : ConnectionState.Connected;

                        //set opponent
                        _opponent = opponent;

                        //fire event?
                        if (!pending)
                            fireEvent = true;

                        //success
                        return true;
                    }
                }
                finally
                {
                    if (fireEvent)
                        OpponentConnected?.InvokeFromTask(opponent);
                }
            }
            catch (Exception ex)
            {
                //_connectionState = ConnectionState.Error;
                Log.Write("SetOpponentAndConnect: Unknown error");
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// Sends invite request to opponent, waits for response or timeout.
        /// </summary>
        public CommandResult InviteOpponent()
        {
            try
            {
                bool fireEvent = false;
                try
                {
                    lock (_inviteLock)
                    {
                        //no opponent?
                        if (_opponent == null)
                            throw new Exception("No opponent set");

                        //send connect-request command
                        byte[] data = PacketBuilder.ToBytes(new object[] { _opponent.Name });
                        CommandResult result = SendCommandRequest(CommandType.ConnectToPlayer, data, TimeSpan.FromSeconds(INVITE_TIMEOUT_SEC));

                        //accept
                        if (result == CommandResult.Accept)
                        {
                            _pendingOpponent = null;
                            //_lastHeartbeatReceived = DateTime.Now;
                            //_connectionState = ConnectionState.Connected;
                            fireEvent = true;
                        }

                        //reject
                        else if (result == CommandResult.Reject)
                        {
                            _opponent = null;
                            _pendingOpponent = null;
                            //_connectionState = ConnectionState.NotConnected;
                            //_dataClient.Disconnect();
                        }

                        //timeout
                        else if (result == CommandResult.Timeout)
                        {
                            _opponent = null;
                            _pendingOpponent = null;
                            //_connectionState = ConnectionState.NotConnected;
                            //_dataClient.Disconnect();
                        }

                        //error
                        else if (result == CommandResult.Error)
                        {
                            _opponent = null;
                            _pendingOpponent = null;
                            //_connectionState = ConnectionState.Error;
                            //_dataClient.Disconnect();
                        }

                        //return
                        return result;
                    }
                }
                finally
                {
                    if (fireEvent)
                        OpponentConnected?.InvokeFromTask(_opponent);
                }
            }
            catch (Exception ex)
            {
                //_connectionState = ConnectionState.Error;
                Log.Write("InviteOpponent: Unknown error");
                ErrorHandler.LogError(ex);
                return CommandResult.Error;
            }
        }

        /// <summary>
        /// Accepts an invite from a remote opponent.
        /// </summary>
        public bool AcceptInviteAndConnect(Player opponent)
        {
            try
            {
                lock (_inviteLock)
                {
                    //return if opponents don't match
                    if ((_pendingOpponent == null) || (opponent.IP != _pendingOpponent.IP))
                        return false;

                    //set opponent and connect
                    //_lastHeartbeatReceived = DateTime.Now;
                    bool success = SetOpponentAndConnect(opponent, false);
                    if (!success)
                    {
                        _opponent = null;
                        _pendingOpponent = null;
                        return false;
                    }

                    //send acceptance
                    return SendCommandResponse(
                        type: CommandType.ConnectToPlayer,
                        sequence: opponent.InviteSequence,
                        result: CommandResult.Accept,
                        data: null);
                }
            }
            catch (Exception ex)
            {
                //_connectionState = ConnectionState.Error;
                Log.Write("AcceptInviteAndConnect: Unknown error");
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// Connects to opponent in order to respond, sends rejection, disconnects.
        /// </summary>
        public bool RejectInvite(Player opponent)
        {
            try
            {
                lock (_inviteLock)
                {
                    //return if opponents don't match
                    if ((_pendingOpponent == null) || (opponent.IP != _pendingOpponent.IP))
                        return false;

                    ////close any existing connection
                    //_dataClient.Disconnect();
                    //_connectionState = ConnectionState.NotConnected;

                    ////connect to opponent
                    //_dataClient.Connect(opponent.IP.ToString(), _config.GamePort);
                    //_dataClient.TcpClient.SendTimeout = 2000;
                    //_dataClient.TcpClient.ReceiveTimeout = 2000;

                    //send rejection
                    return SendCommandResponse(
                        type: CommandType.ConnectToPlayer,
                        sequence: opponent.InviteSequence,
                        result: CommandResult.Reject,
                        data: null);
                }
            }
            catch (Exception ex)
            {
                //_connectionState = ConnectionState.Error;
                Log.Write("RejectInvite: Unknown error");
                ErrorHandler.LogError(ex);
                return false;
            }
            finally
            {
                try
                {
                    //_dataClient.Disconnect();
                    //_connectionState = ConnectionState.NotConnected;
                }
                catch (Exception ex)
                {
                    Log.Write("RejectInvite: Disconnect error");
                    ErrorHandler.LogError(ex);
                }
            }
        }

        /// <summary>
        /// Closes connection to opponent, removes opponent reference.
        /// </summary>
        public bool CloseConnection()
        {
            try
            {
                lock (_inviteLock)
                {
                    ////set flag
                    //_connectionState = ConnectionState.NotConnected;

                    //remove opponent reference
                    _opponent = null;
                    _pendingOpponent = null;

                    ////close connection
                    //_dataClient.Disconnect();

                    //success
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Write("CloseConnection: Unknown error");
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        #endregion

        #region Outgoing

        /// <summary>
        /// Sends command request, blocks until response or timeout.  Data is optional.
        /// </summary>
        public CommandResult SendCommandRequest(CommandType type, byte[] data, TimeSpan timeout)
        {
            try
            {
                //disabled?
                if (_isDisabled)
                    return CommandResult.Error;

                ////no opponent?
                //if (_opponent == null)
                //    throw new Exception("No opponent set");

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
                    gameTitle: _config.GameTitle, gameVersion: _config.GameVersion, sourceIP: _config.LocalIP,
                    destinationIP: _config.ServerIP, destinationPort: _config.ServerPort, playerName: _localPlayer.Name,
                    commandType: type, sequence: sequence, retryAttempt: 0, data: data);

                //send packet
                //byte[] bytes = packet.ToBytes();
                //_dataClient.Write(bytes);
                _client.SendPacket(packet);
                _commandRequestsSent++;

                //record command request has been sent
                _commandManager.RequestSent(packet, timeout);

                //loop
                while (true)
                {
                    //vars
                    DateTime start = DateTime.Now;

                    //get current status
                    CommandResult result = _commandManager.GetCommandStatus(sequence);

                    //have answer or timeout?  return!
                    if (result != CommandResult.Unspecified)
                        return result;

                    //sleep
                    Thread.Sleep(2);
                }
            }
            catch (Exception ex)
            {
                //_connectionState = ConnectionState.Error;
                Log.Write("SendCommandRequest: Error sending data");
                ErrorHandler.LogError(ex);
                return CommandResult.Error;
            }
        }

        /// <summary>
        /// Sends command response.  Data is optional.
        /// </summary>
        public bool SendCommandResponse(CommandType type, ushort sequence, CommandResult result, byte[] data)
        {
            try
            {
                //no opponent?
                Player opponent = type != CommandType.ConnectToPlayer ? _opponent : _pendingOpponent;
                if (opponent == null)
                    throw new Exception("No opponent set");

                //create packet
                CommandResponsePacket packet = new CommandResponsePacket(
                    gameTitle: _config.GameTitle, gameVersion: _config.GameVersion, sourceIP: _config.LocalIP,
                    destinationIP: opponent.IP, destinationPort: _config.ServerPort, playerName: _localPlayer.Name,
                    commandType: type, sequence: sequence, result: result, data: data);

                //send packet
                byte[] bytes = packet.ToBytes();
                //_dataClient.Write(bytes);
                _commandResponsesSent++;

                //success
                return true;
            }
            catch (Exception ex)
            {
                //_connectionState = ConnectionState.Error;
                Log.Write("SendCommandResponse: Error sending data");
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// Sends generic one-way data packet.
        /// </summary>
        public bool SendData(byte[] data)
        {
            try
            {
                //no opponent?
                if (_opponent == null)
                    throw new Exception("No opponent set");

                //create packet
                DataPacket packet = new DataPacket(
                    gameTitle: _config.GameTitle, gameVersion: _config.GameVersion, sourceIP: _config.LocalIP,
                    destinationIP: _config.ServerIP, destinationPort: _config.ServerPort, playerName: _localPlayer.Name,
                    data: data);

                //send packet
                byte[] bytes = packet.ToBytes();
                //_dataClient.Write(bytes);
                _dataSent++;

                //success
                return true;
            }
            catch (Exception ex)
            {
                //_connectionState = ConnectionState.Error;
                Log.Write("SendData: Error sending data");
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// Sends heartbeat packet to opponent endpoint.
        /// </summary>
        public bool SendHeartbeat()
        {
            try
            {
                ////no opponent?
                //if (_opponent == null)
                //    throw new Exception("No opponent set");

                //create packet
                HeartbeatPacket packet = new HeartbeatPacket(
                    gameTitle: _config.GameTitle, gameVersion: _config.GameVersion, sourceIP: _config.LocalIP,
                    destinationIP: _config.ServerIP, destinationPort: _config.ServerPort, playerName: _localPlayer.Name,
                    count: _heartbeatsSent + 1);

                //send packet
                _client.SendPacket(packet);
                _heartbeatsSent++;

                //success
                return true;
            }
            catch (Exception ex)
            {
                //_connectionState = ConnectionState.Error;
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
                    _pendingOpponent = new Player(p2.SourceIP, p2.GameTitle, p2.GameVersion, playerName, p2.Sequence);
                    OpponentInviteReceived?.InvokeFromTask(_pendingOpponent);
                    return;
                }

                //reject if wrong opponent
                if ((!packet.SourceIP.Equals(_config.ServerIP)) && ((_opponent == null) || (!packet.SourceIP.Equals(_opponent.IP))))
                    return;

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
                //_connectionState = ConnectionState.Error;
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

                        //heartbeat packet
                        else if (packet is HeartbeatPacket p4)
                        {
                            //increment counter
                            _heartbeatsReceived++;

                            ////record time
                            //_lastHeartbeatReceived = DateTime.Now;
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
                ////too long since heartbeat received?
                //if ((_connectionState == ConnectionState.Connected) && (TimeSinceLastHeartbeatReceived.TotalSeconds > 5))
                //{
                //    Log.Write("Disconnect: Heatbeat timeout");
                //    _connectionState = ConnectionState.NotConnected;
                //    _opponent = null;
                //    _pendingOpponent = null;
                //    _dataClient.Disconnect();
                //    Disconnected?.InvokeFromTask();
                //    return;
                //}

                ////connection broken?
                //if ((_connectionState == ConnectionState.Connected) && (_dataClient.TcpClient?.Connected != true))
                //{
                //    Log.Write("Disconnect: Connection broken");
                //    _connectionState = ConnectionState.NotConnected;
                //    _opponent = null;
                //    _pendingOpponent = null;
                //    _dataClient.Disconnect();
                //    Disconnected?.InvokeFromTask();
                //    return;
                //}

                ////error state?
                //if (_connectionState == ConnectionState.Error)
                //{
                //    Log.Write("Disconnect: Transmission error");
                //    _connectionState = ConnectionState.NotConnected;
                //    _opponent = null;
                //    _pendingOpponent = null;
                //    _dataClient.Disconnect();
                //    Disconnected?.InvokeFromTask();
                //    return;
                //}
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
            DateTime lastSend = DateTime.MinValue;
            while (true)
            {
                try
                {
                    //exit if stopped
                    if (_isStopped)
                        return;

                    //calculate wait
                    TimeSpan elapsed = DateTime.Now - lastSend;
                    int sleepMs = Math.Max(250 - (int)elapsed.TotalMilliseconds, 0);

                    //sleep
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
        NotConnected,
        Connected_PendingInviteAcceptance,
        Connected,
        Error
    }
}

using Common.Configuration;
using Common.Error;
using Common.Extensions;
using Common.Networking.Game.Discovery;
using Common.Networking.Game.Packets;
using Common.Threading;
using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Networking.Game
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
        private const int INVITE_TIMEOUT_SEC = 120;

        //private
        private readonly IConfig _config;
        private readonly IErrorHandler _errorHandler;
        private readonly Player _localPlayer;
        private readonly DiscoveryClient _discoveryClient;
        private readonly DiscoveryServer _discoveryServer;
        private readonly DiscoveredPlayers _discoveredPlayers;
        private readonly SimpleTcpClient _dataClient;
        private readonly SimpleTcpServer _dataServer;
        private readonly List<byte> _incomingBuffer;
        private readonly CommandManager _commandManager;
        private readonly SimpleTimer _maintenanceTimer;
        private readonly List<PacketBase> _incomingPackets;
        private readonly ManualResetEventSlim _incomingPacketSignal;
        private readonly Thread _incomingPacketThread;
        private readonly Thread _heartbeatThread;
        private readonly object _inviteLock = new object();
        private ushort _commandSequence;
        private ConnectionState _connectionState;
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
        private DateTime _lastHeartbeatReceived;
        private bool _isStarted = false;
        private bool _isStopped = false;

        //public
        public string GameTitle => _config.GameTitle;
        public Version GameVersion => _config.GameVersion;
        public IPAddress LocalIP => _config.LocalIP;
        public ushort GamePort => _config.GamePort;
        public Player LocalPlayer => _localPlayer;
        public Player Opponent => _opponent;
        public ConnectionState ConnectionState => _connectionState;
        public long HeartbeatsSent => _heartbeatsSent;
        public long HeartbeatsReceived => _heartbeatsReceived;
        public long DataSent => _dataSent;
        public long DataReceived => _dataReceived;
        public long CommandRequestsSent => _commandRequestsSent;
        public long CommandRequestsReceived => _commandRequestsReceived;
        public long CommandResponsesSent => _commandResponsesSent;
        public long CommandResponsesReceived => _commandResponsesReceived;
        public DateTime LastHeartbeatReceived => _lastHeartbeatReceived;
        public TimeSpan TimeSinceLastHeartbeatReceived => DateTime.Now - _lastHeartbeatReceived;

        //events
        public event Action<Player> OpponentConnected;
        public event Action<Player> OpponentInviteReceived;
        public event Action<CommandRequestPacket> CommandRequestPacketReceived;
        public event Action<CommandResponsePacket> CommandResponsePacketReceived;
        public event Action<DataPacket> DataPacketReceived;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public GameCommunications(IConfig config, string playerName, IErrorHandler errorHandler = null)
        {
            //vars
            _errorHandler = errorHandler;
            _config = config;
            _localPlayer = new Player(config.GameTitle, config.GameVersion, config.LocalIP, config.GamePort, playerName);
            _discoveryClient = new DiscoveryClient(config.GameTitle, config.GameVersion, config.LocalIP, config.GamePort, playerName, errorHandler);
            _discoveryServer = new DiscoveryServer(config.GamePort, errorHandler);
            _discoveredPlayers = new DiscoveredPlayers();
            _dataClient = new SimpleTcpClient();
            _dataServer = new SimpleTcpServer();
            _incomingBuffer = new List<byte>();
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
            _connectionState = ConnectionState.NotConnected;
            _opponent = null;
            _pendingOpponent = null;
            _heartbeatsSent = 0;
            _heartbeatsReceived = 0;
            _lastHeartbeatReceived = DateTime.MinValue;

            //events
            _discoveryServer.PlayerAnnounced += (p) => _discoveredPlayers.AddOrUpdatePlayer(p);
            _dataServer.DataReceived += (s, m) => IncomingData(m?.Data);
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
            _discoveryServer.Dispose();
            _discoveryClient.Dispose();
            _dataClient.Dispose();
            _commandManager.Dispose();
            _maintenanceTimer.Dispose();
        }

        #region Start / Stop

        /// <summary>
        /// Starts discovery server (UDP), discovery client (UDP broadbast), and data server (TCP).
        /// </summary>
        public void Start()
        {
            //prevent restart
            if (_isStarted)
                throw new Exception("Cannot restart communications");
            _isStarted = true;

            //start discovery server
            _discoveryServer.Start();

            //start discovery broadcast client
            _discoveryClient.Start();

            //start incoming packet thread
            _incomingPacketThread.Start();

            //start data server
            _dataServer.Start(_config.GamePort);

            //start maintenance timer
            _maintenanceTimer.Start();

            //start heartbeat thread
            _heartbeatThread.Start();
        }

        /// <summary>
        /// Stops everything.
        /// </summary>
        public void Stop()
        {
            //prevent restop
            if (_isStopped)
                return;
            _isStopped = true;

            //stop maintenance timer
            _maintenanceTimer.Stop();

            //client disconnect from server
            _dataClient.Disconnect();

            //server stop receiving
            _dataServer.Stop();

            //stop discovery client
            _discoveryClient.Stop();

            //stop discovery server
            _discoveryServer.Stop();
        }

        #endregion

        #region Player Discovery

        /// <summary>
        /// Changes the broadcasted player's name.
        /// </summary>
        public void ChangePlayerName(string name)
        {
            _discoveryClient.PlayerName = name;
            _localPlayer.Name = name;
        }

        /// <summary>
        /// Returns list of most recent discovered players, for this game version.
        /// </summary>
        public IReadOnlyList<Player> GetDiscoveredPlayers(int top = 5)
        {
            return _discoveredPlayers.GetPlayers(_config.GameTitle, _config.GameVersion, _config.LocalIP, top);
        }

        /// <summary>
        /// Returns count of discovered players, for this game version.
        /// </summary>
        public int GetDiscoveredPlayerCount(int top = 5)
        {
            return _discoveredPlayers.GetPlayerCount(_config.GameTitle, _config.GameVersion, _config.LocalIP, top);
        }

        #endregion

        #region Opponent Connect and Invites

        /// <summary>
        /// Sets expected opponent player and opens connection, regardless of whether they have accepted invite.  
        /// Allows invite and other communications to be sent.  Set pending to false, if called by receiving side
        /// after *they've* accepted the invite.
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
                        //close any existing connection
                        _dataClient.Disconnect();
                        _connectionState = ConnectionState.NotConnected;

                        //connect to opponent
                        _dataClient.Connect(opponent.IP.ToString(), _config.GamePort);
                        _connectionState = pending ? ConnectionState.Connected_PendingInviteAcceptance : ConnectionState.Connected;

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
                        OpponentConnected?.Invoke(opponent);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
                _connectionState = ConnectionState.Error;
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
                        CommandResult result = SendCommandRequest(1, data, TimeSpan.FromSeconds(INVITE_TIMEOUT_SEC));

                        //accept
                        if (result == CommandResult.Accept)
                        {
                            _connectionState = ConnectionState.Connected;
                            fireEvent = true;
                        }

                        //reject
                        else if (result == CommandResult.Reject)
                        {
                            _opponent = null;
                            _connectionState = ConnectionState.NotConnected;
                            _dataClient.Disconnect();
                        }

                        //timeout
                        else if (result == CommandResult.Timeout)
                        {
                            _opponent = null;
                            _connectionState = ConnectionState.NotConnected;
                            _dataClient.Disconnect();
                        }

                        //error
                        else if (result == CommandResult.Error)
                        {
                            _opponent = null;
                            _connectionState = ConnectionState.Error;
                            _dataClient.Disconnect();
                        }

                        //return
                        return result;
                    }
                }
                finally
                {
                    if (fireEvent)
                        OpponentConnected?.Invoke(_opponent);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
                _connectionState = ConnectionState.Error;
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
                    bool success = SetOpponentAndConnect(opponent, false);
                    if (!success)
                        return false;

                    //send acceptance
                    return SendCommandResponse(
                        type: 1,
                        sequence: opponent.InviteSequence,
                        result: CommandResult.Accept,
                        data: null);
                }
            }
            catch (Exception ex)
            {
                _errorHandler.LogError(ex);
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

                    //close any existing connection
                    _dataClient.Disconnect();
                    _connectionState = ConnectionState.NotConnected;

                    //connect to opponent
                    _dataClient.Connect(opponent.IP.ToString(), _config.GamePort);
                    _connectionState = ConnectionState.Connected;

                    //send rejection
                    return SendCommandResponse(
                        type: 1,
                        sequence: opponent.InviteSequence,
                        result: CommandResult.Reject,
                        data: null);
                }
            }
            catch (Exception ex)
            {
                _errorHandler.LogError(ex);
                return false;
            }
            finally
            {
                try
                {
                    _dataClient.Disconnect();
                    _connectionState = ConnectionState.NotConnected;
                }
                catch (Exception ex)
                {
                    _errorHandler.LogError(ex);
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
                    //set flag
                    _connectionState = ConnectionState.NotConnected;

                    //remove opponent reference
                    _opponent = null;

                    //close connection
                    _dataClient.Disconnect();

                    //success
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorHandler.LogError(ex);
                return false;
            }
        }

        #endregion

        #region Outgoing

        /// <summary>
        /// Sends command request, blocks until response or timeout.  Data is optional.
        /// </summary>
        public CommandResult SendCommandRequest(ushort type, byte[] data, TimeSpan timeout)
        {
            try
            {
                //no opponent?
                if (_opponent == null)
                    throw new Exception("No opponent set");

                //reconnect if not connected
                if (_dataClient.TcpClient?.Connected != true)
                    _dataClient.Connect(_opponent.IP.ToString(), _config.GamePort);

                //vars
                ushort sequence;
                lock (this)
                {
                    _commandSequence++;
                    if (_commandSequence >= UInt16.MaxValue)
                        _commandSequence = 1;
                    sequence = _commandSequence;
                }
                ushort retyAttempt = 0;

                //loop
                while (true)
                {
                    //create packet
                    CommandRequestPacket packet = new CommandRequestPacket(
                        gameTitle: _config.GameTitle, gameVersion: _config.GameVersion, sourceIP: _config.LocalIP,
                        destinationIP: _opponent.IP, destinationPort: _config.GamePort, commandType: type,
                        sequence: sequence, retryAttempt: retyAttempt++, data: data);

                    //send packet
                    byte[] bytes = packet.ToBytes();
                    _dataClient.Write(bytes);
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

                        //break if time to retry packet
                        if ((DateTime.Now - start).TotalMilliseconds >= 250)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
                _connectionState = ConnectionState.Error;
                return CommandResult.Error;
            }
        }

        /// <summary>
        /// Sends command response.  Data is optional.
        /// </summary>
        public bool SendCommandResponse(ushort type, ushort sequence, CommandResult result, byte[] data)
        {
            try
            {
                //no opponent?
                if (_opponent == null)
                    throw new Exception("No opponent set");

                //reconnect if not connected
                if (_dataClient.TcpClient?.Connected != true)
                    _dataClient.Connect(_opponent.IP.ToString(), _config.GamePort);

                //create packet
                CommandResponsePacket packet = new CommandResponsePacket(
                    gameTitle: _config.GameTitle, gameVersion: _config.GameVersion, sourceIP: _config.LocalIP,
                    destinationIP: _opponent.IP, destinationPort: _config.GamePort, commandType: type,
                    sequence: sequence, result: result, data: data);

                //send packet
                byte[] bytes = packet.ToBytes();
                _dataClient.Write(bytes);
                _commandResponsesSent++;

                //success
                return true;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
                _connectionState = ConnectionState.Error;
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

                //reconnect if not connected
                if (_dataClient.TcpClient?.Connected != true)
                    _dataClient.Connect(_opponent.IP.ToString(), _config.GamePort);

                //create packet
                DataPacket packet = new DataPacket(
                    gameTitle: _config.GameTitle, gameVersion: _config.GameVersion, sourceIP: _config.LocalIP,
                    destinationIP: _opponent.IP, destinationPort: _config.GamePort, data: data);

                //send packet
                byte[] bytes = packet.ToBytes();
                _dataClient.Write(bytes);
                _dataSent++;

                //success
                return true;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
                _connectionState = ConnectionState.Error;
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
                //no opponent?
                if (_opponent == null)
                    throw new Exception("No opponent set");

                //reconnect if not connected
                if (_dataClient.TcpClient?.Connected != true)
                    _dataClient.Connect(_opponent.IP.ToString(), _config.GamePort);

                //create packet
                HeartbeatPacket packet = new HeartbeatPacket(
                    gameTitle: _config.GameTitle, gameVersion: _config.GameVersion, sourceIP: _config.LocalIP,
                    destinationIP: _opponent.IP, destinationPort: _config.GamePort, count: _heartbeatsSent + 1);

                //send packet
                byte[] bytes = packet.ToBytes();
                _dataClient.Write(bytes);
                _heartbeatsSent++;

                //success
                return true;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
                _connectionState = ConnectionState.Error;
                return false;
            }
        }

        #endregion

        #region Incomming

        /// <summary>
        /// Fired when server object receives data from any client.  No restrictions
        /// on who can connect, but packets that don't match are discarded.
        /// </summary>
        private void IncomingData(byte[] buffer)
        {
            try
            {
                //reject if no data
                if (buffer == null)
                    return;

                //vars
                List<byte[]> packets = new List<byte[]>();

                //lock buffer
                lock (_incomingBuffer)
                {
                    //buffer overflow?
                    if (_incomingBuffer.Count > 1000000)
                        _incomingBuffer.Clear();

                    //add to buffer
                    _incomingBuffer.AddRange(buffer);

                    //loop
                    while (true)
                    {
                        //break if zero bytes
                        if (_incomingBuffer.Count == 0)
                            break;

                        //find first four matching footer bytes (terminator)
                        int firstIndex = FindToken(_incomingBuffer, PacketBase.PACKET_FOOTER);

                        //break if no footer
                        if (firstIndex == -1)
                            break;

                        //dequeue bytes
                        int count = firstIndex + 4;
                        byte[] bytes = _incomingBuffer.Dequeue(count).ToArray();

                        //add to list
                        packets.Add(bytes);
                    }
                }

                //loop through packet (candidates)
                bool added = false;
                foreach (byte[] bytes in packets)
                {
                    //reject if invalid
                    PacketBase packet = PacketBase.FromBytes(bytes);
                    if (packet == null)
                        continue;

                    //reject if wrong game or version
                    if ((packet.GameTitle != _config.GameTitle) || (packet.GameVersion != _config.GameVersion))
                        continue;

                    //special logic for invite requests
                    if ((packet is CommandRequestPacket p1) && (p1.CommandType == 1))
                    {
                        _commandRequestsReceived++;
                        PacketParser parser = new PacketParser(p1.Data);
                        string playerName = parser.GetString();
                        _pendingOpponent = new Player(p1.GameTitle, p1.GameVersion, p1.SourceIP, _config.GamePort, playerName, p1.Sequence);
                        Task.Run(() => OpponentInviteReceived?.Invoke(_pendingOpponent));
                        continue;
                    }

                    //reject if wrong opponent
                    if ((_opponent == null) || (!packet.SourceIP.Equals(_opponent.IP)))
                        continue;

                    //queue packet for processing
                    lock (_incomingPackets)
                    {
                        _incomingPackets.Add(packet);
                        added = true;
                    }
                }

                //signal
                if (added)
                    _incomingPacketSignal.Set();
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
                _connectionState = ConnectionState.Error;
            }
        }

        /// <summary>
        /// Finds and returns first index of matching four-byte pattern defined by specified token.
        /// Returns -1 if not found.
        /// </summary>
        private static int FindToken(IList<byte> buffer, int token)
        {
            byte[] tokenBytes = BitConverter.GetBytes(token);
            for (int i = 0; i < buffer.Count - 3; i++)
            {
                if ((buffer[i] == tokenBytes[0])
                    && (buffer[i + 1] == tokenBytes[1])
                    && (buffer[i + 2] == tokenBytes[2])
                    && (buffer[i + 3] == tokenBytes[3]))
                {
                    return i;
                }
            }
            return -1;
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
                            CommandRequestPacketReceived?.Invoke(p1);
                        }

                        //command response packet
                        else if (packet is CommandResponsePacket p2)
                        {
                            //increment counter
                            _commandResponsesReceived++;

                            //record command response received
                            _commandManager.ResponseReceived(p2);

                            //fire event
                            CommandResponsePacketReceived?.Invoke(p2);
                        }

                        //data packet
                        else if (packet is DataPacket p3)
                        {
                            //increment counter
                            _dataReceived++;

                            //fire event
                            DataPacketReceived?.Invoke(p3);
                        }

                        //heartbeat packet
                        else if (packet is HeartbeatPacket p4)
                        {
                            //increment counter
                            _heartbeatsReceived++;

                            //record time
                            _lastHeartbeatReceived = DateTime.Now;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _errorHandler?.LogError(ex);
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
                //determine if error state
                if ((_opponent != null) && (_connectionState == ConnectionState.Connected))
                    if (_dataClient?.TcpClient?.Connected != true)
                        _connectionState = ConnectionState.Error;

                //if error, close and reconnect
                if ((_opponent != null) && (_connectionState == ConnectionState.Error))
                {
                    _dataClient.Disconnect();
                    _dataClient.Connect(_opponent.IP.ToString(), _config.GamePort);
                    _connectionState = ConnectionState.Connected;
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
            }
        }

        /// <summary>
        /// Heartbeat thread, sends heartbeat packets every ~100ms.
        /// </summary>
        private void Heartbeat_Thread()
        {
            while (true)
            {
                try
                {
                    //exit if stopped
                    if (_isStopped)
                        return;

                    //sleep 100ms
                    Thread.Sleep(100);

                    //continue if no opponent
                    if (_opponent == null)
                        continue;

                    //send heartbeat to opponent
                    SendHeartbeat();
                }
                catch (Exception ex)
                {
                    _errorHandler?.LogError(ex);
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

using Common.Error;
using Common.Networking.Simple.Discovery;
using Common.Networking.Simple.Packets;
using Common.Threading;
using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Common.Networking.Simple
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
        private const int CONNECT_REPLY_TIMEOUT_MS = 10000;

        //private
        private readonly IErrorHandler _errorHandler;
        private readonly string _gameTitle;
        private readonly Version _gameVersion;
        private readonly IPAddress _localIP;
        private readonly ushort _gamePort;
        private string _localPlayerName;
        private readonly DiscoveryClient _discoveryClient;
        private readonly DiscoveryServer _discoveryServer;
        private readonly DiscoveredPlayers _discoveredPlayers;
        private readonly SimpleTcpClient _dataClient;
        private readonly SimpleTcpServer _dataServer;
        private readonly CommandManager _commandManager;
        private readonly SimpleTimer _timer;
        private ConnectionState _connectionState;
        private ushort _commandSequence;
        private IPAddress _remoteIP;
        private Player _remotePlayer;

        //public
        public string GameTitle => _gameTitle;
        public Version GameVersion => _gameVersion;
        public IPAddress LocalIP => _localIP;
        public ushort GamePort => _gamePort;
        public string LocalPlayerName { get => _localPlayerName; set => _localPlayerName = value; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public GameCommunications(string gameTitle, Version gameVersion, IPAddress localIP, ushort gamePort, string playerName, IErrorHandler errorHandler = null)
        {
            //vars
            _errorHandler = errorHandler;
            _gameTitle = gameTitle;
            _gameVersion = gameVersion;
            _localIP = localIP;
            _gamePort = gamePort;
            _localPlayerName = playerName;
            _discoveryClient = new DiscoveryClient(gameTitle, gameVersion, localIP, gamePort, playerName, errorHandler);
            _discoveryServer = new DiscoveryServer(gamePort, errorHandler);
            _discoveredPlayers = new DiscoveredPlayers();
            _dataClient = new SimpleTcpClient();
            _dataServer = new SimpleTcpServer();
            _commandManager = new CommandManager();
            _timer = new SimpleTimer(Timer_Callback, 15, false);
            _connectionState = ConnectionState.NotConnected;
            _commandSequence = 0;
            _remoteIP = null;
            _remotePlayer = null;

            //events
            _discoveryServer.PlayerAnnounced += (p) => _discoveredPlayers.AddOrUpdatePlayer(p);
            _dataServer.DataReceived += (s, m) => DataReceived(m?.Data);
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
            _timer.Dispose();
        }

        /// <summary>
        /// Starts discovery server (UDP), discovery client (UDP broadbast), and data server (TCP).
        /// </summary>
        public void Start()
        {
            //start discovery server
            _discoveryServer.Start();

            //start discovery broadcast client
            _discoveryClient.Start();

            //start data server
            _dataServer.Start(_gamePort);

            //start maintenance timer
            _timer.Start();
        }

        /// <summary>
        /// Stops everything.
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
            _dataClient.Disconnect();
            _dataServer.Stop();
            _discoveryClient.Stop();
            _discoveryServer.Stop();
        }

        /// <summary>
        /// Changes the broadcasted player's name.
        /// </summary>
        public void ChangePlayerName(string name)
        {
            _discoveryClient.PlayerName = name;
        }

        /// <summary>
        /// Returns list of most recent discovered players, for this game version.
        /// </summary>
        public IReadOnlyList<Player> GetDiscoveredPlayers(int top = 5)
        {
            return _discoveredPlayers.GetPlayers(_gameTitle, _gameVersion, _localIP, top);
        }

        /// <summary>
        /// Returns count of discovered players, for this game version.
        /// </summary>
        public int GetDiscoveredPlayerCount(int top = 5)
        {
            return _discoveredPlayers.GetPlayerCount(_gameTitle, _gameVersion, _localIP, top);
        }

        /// <summary>
        /// Tries to connect to player to start new two-player game.  Connection
        /// might fail or timeout, or player might reject invite.
        /// </summary>
        public CommandResult ConnectToPlayer(Player player)
        {
            CommandResult result = CommandResult.Unspecified;
            try
            {
                //close any existing connection
                _remotePlayer = null;
                _dataClient.Disconnect();
                _connectionState = ConnectionState.NotConnected;

                //connect socket
                _dataClient.Connect(player.IP.ToString(), _gamePort);
                _remoteIP = player.IP;

                //send connect-request command
                _connectionState = ConnectionState.PendingAcceptance_SendSide;
                result = SendCommand(1, TimeSpan.FromMilliseconds(CONNECT_REPLY_TIMEOUT_MS));

                //do stuff..
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
                _connectionState = ConnectionState.Error;
            }
            return result;
        }

        /// <summary>
        /// Sends command, blocks until response or timeout.
        /// </summary>
        public CommandResult SendCommand(ushort type, TimeSpan timeout)
        {
            try
            {
                //not connected?
                if (type == 1)
                {
                    if ((_connectionState != ConnectionState.Connected) && (_connectionState != ConnectionState.PendingAcceptance_SendSide))
                        throw new Exception("Data client not connected");
                }
                else
                {
                    if (_connectionState != ConnectionState.Connected)
                        throw new Exception("Data client not connected");
                }

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
                        gameTitle: _gameTitle, gameVersion: _gameVersion, sourceIP: _localIP, 
                        destinationIP: _remoteIP, destinationPort: _gamePort, commandType: type, 
                        sequence: sequence, retryAttempt: retyAttempt++);

                    //send packet
                    byte[] bytes = packet.ToBytes();
                    _dataClient.Write(bytes);

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
            }
            return CommandResult.Error;
        }

        
        private void DataReceived(byte[] bytes)
        {
            try
            {
                if (bytes == null)
                    return;
                
                PacketBase packet = PacketBase.FromBytes(bytes);
                if (packet == null)
                    return;

                if ((packet.GameTitle != _gameTitle) || (packet.GameVersion != _gameVersion))
                    return;

                if (packet.Type == PacketType.CommandRequest)
                {
                    CommandRequestPacket p = (CommandRequestPacket)packet;
                    if (p.CommandType == 1)
                    {

                    }

                }


            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
                _connectionState = ConnectionState.Error;
            }
        }

        /// <summary>
        /// Fired by timer, performs maintenance.
        /// </summary>
        private void Timer_Callback()
        {
            try
            {
                //determine if error state
                if (_connectionState == ConnectionState.Connected)
                    if (_dataClient?.TcpClient?.Connected != true)
                        _connectionState = ConnectionState.Error;

                //if error, close and reconnect
                if (_connectionState == ConnectionState.Error)
                {
                    _remotePlayer = null;
                    _dataClient.Disconnect();
                    if (_remoteIP != null)
                        _dataClient.Connect(_remoteIP.ToString(), _gamePort);
                    else
                        _connectionState = ConnectionState.NotConnected;
                    return;
                }

                //receive-side accepted
                if (_connectionState == ConnectionState.PendingAcceptance_ReceiveSide)
                {
                    //connect to send-side server

                }



            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
            }
        }

    }

    /// <summary>
    /// Represents the system's connection states.
    /// </summary>
    public enum ConnectionState
    {
        NotConnected,
        Error,
        PendingAcceptance_SendSide,
        PendingAcceptance_ReceiveSide,
        Connected
    }
}

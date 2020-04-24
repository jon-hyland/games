using Common.Error;
using Common.Networking.Simple.Discovery;
using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Net;

namespace Common.Networking.Simple
{
    public class GameCommunications
    {
        //private
        private readonly IErrorHandler _errorHandler;
        private readonly string _gameTitle;
        private readonly Version _gameVersion;
        private readonly IPAddress _localIP;
        private readonly int _localPort;
        private string _playerName;
        private readonly DiscoveryClient _discoveryClient;
        private readonly DiscoveryServer _discoveryServer;
        private readonly DiscoveredPlayers _discoveredPlayers;
        private readonly SimpleTcpClient _dataClient;
        private readonly SimpleTcpServer _dataServer;

        //public
        public string GameTitle => _gameTitle;
        public Version GameVersion => _gameVersion;
        public IPAddress LocalIP => _localIP;
        public int LocalPort => _localPort;
        public string PlayerName { get => _playerName; set => _playerName = value; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public GameCommunications(string gameTitle, Version gameVersion, IPAddress localIP, int localPort, string playerName, IErrorHandler errorHandler = null)
        {
            //vars
            _errorHandler = errorHandler;
            _gameTitle = gameTitle;
            _gameVersion = gameVersion;
            _localIP = localIP;
            _localPort = localPort;
            _playerName = playerName;
            _discoveryClient = new DiscoveryClient(gameTitle, gameVersion, localIP, localPort, playerName, errorHandler);
            _discoveryServer = new DiscoveryServer(localPort, errorHandler);
            _discoveredPlayers = new DiscoveredPlayers();
            _dataClient = new SimpleTcpClient();
            _dataServer = new SimpleTcpServer();

            //events
            _discoveryServer.PlayerAnnounced += (p) => _discoveredPlayers.AddOrUpdatePlayer(p);
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
            _dataServer.Start(_localPort);
        }

        /// <summary>
        /// Stops everything.
        /// </summary>
        public void Stop()
        {
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

    }
}

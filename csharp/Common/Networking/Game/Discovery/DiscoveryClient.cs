using Common.Error;
using Common.Networking.Game.Packets;
using Common.Threading;
using System;
using System.Net;
using System.Net.Sockets;

namespace Common.Networking.Game.Discovery
{
    /// <summary>
    /// Sends player discovery packets over UDP broadcast, to inform other game instances
    /// of your presence.
    /// </summary>
    public class DiscoveryClient : IDisposable
    {
        //private
        private readonly IErrorHandler _errorHandler;
        private readonly SimpleTimer _timer;
        private readonly UdpClient _client;
        private readonly IPEndPoint _endPoint;
        private readonly string _gameTitle;
        private readonly Version _gameVersion;
        private readonly IPAddress _localIP;
        private readonly ushort _localPort;
        private string _playerName;

        //public
        public string GameTitle => _gameTitle;
        public Version GameVersion => _gameVersion;
        public IPAddress LocalIP => _localIP;
        public ushort LocalPort => _localPort;
        public string PlayerName { get => _playerName; set => _playerName = value; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public DiscoveryClient(string gameTitle, Version gameVersion, IPAddress localIP, ushort localPort, string playerName, IErrorHandler errorHandler = null)
        {
            _errorHandler = errorHandler;
            _gameTitle = gameTitle;
            _gameVersion = gameVersion;
            _localIP = localIP;
            _localPort = localPort;
            _playerName = playerName;
            
            _timer = new SimpleTimer(SendPacket, 1000, false, true, true);
            _client = new UdpClient(new IPEndPoint(localIP, 0))
            {
                EnableBroadcast = true
            };
            _endPoint = new IPEndPoint(IPAddress.Broadcast, localPort);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            _timer?.Dispose();
            _client?.Dispose();
        }

        /// <summary>
        /// Starts broadcasting.
        /// </summary>
        public void Start()
        {
            _timer.Start();
        }

        /// <summary>
        /// Stops broadcasting.
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
        }

        /// <summary>
        /// Performs IP self-discovery and sends broadcast UDP packet.
        /// </summary>
        private void SendPacket()
        {
            try
            {
                DiscoveryPacket p = new DiscoveryPacket(
                    gameTitle: _gameTitle, gameVersion: _gameVersion, sourceIP: _localIP, 
                    destinationIP: IPAddress.Broadcast, destinationPort: _localPort, 
                    playerIP: _localIP, playerPort: _localPort, playerName: _playerName);

                byte[] bytes = p.ToBytes();
                _client.Send(bytes, bytes.Length, _endPoint);
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
            }
        }
    }
}

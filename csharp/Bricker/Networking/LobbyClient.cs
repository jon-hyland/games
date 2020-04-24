using Bricker.Configuration;
using Bricker.Error;
using Common.Threading;
using System;
using System.Net;
using System.Net.Sockets;

namespace Bricker.Networking
{
    /// <summary>
    /// Sends discovery packets over UDP broadcast, for use in two-player lobby.
    /// </summary>
    public class LobbyClient : IDisposable
    {
        //private
        private readonly SimpleTimer _timer;
        private readonly UdpClient _client;
        private readonly IPEndPoint _endPoint;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public LobbyClient()
        {
            _timer = new SimpleTimer(SendPacket, 1000, false, true, true);
            //_client = new UdpClient()
            //{
            //    EnableBroadcast = true
            //};
            _client = new UdpClient(new IPEndPoint(IPAddress.Parse(Config.LocalIP), 0))
            {
                EnableBroadcast = true
            };
            _endPoint = new IPEndPoint(IPAddress.Broadcast, 8710);
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
                Packet p = new Packet(Config.LocalIP, Config.Initials);
                byte[] bytes = p.ToBytes();
                _client.Send(bytes, bytes.Length, _endPoint);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }
    }
}

using Bricker.Configuration;
using Bricker.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Bricker.Networking
{
    /// <summary>
    /// Receives discovery packets over UDP broadcast, for use in two-player lobby.
    /// </summary>
    public class LobbyServer
    {
        //private
        private readonly UdpClient _server;
        private bool _stop;
        private bool _disposed;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public LobbyServer()
        {
            _server = new UdpClient();
            _stop = true;
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            _server?.Dispose();
        }

        /// <summary>
        /// Starts broadcasting.
        /// </summary>
        public void Start()
        {
            _stop = false;
            _server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _server.Client.Bind(new IPEndPoint(IPAddress.Any, 8710));
            _server.BeginReceive(new AsyncCallback(OnReceive), null);
        }

        /// <summary>
        /// Stops broadcasting.
        /// </summary>
        public void Stop()
        {
            _stop = true;
        }

        /// <summary>
        /// Fired when packet received.
        /// </summary>
        private void OnReceive(IAsyncResult result)
        {
            try
            {
                IPEndPoint ep = null;
                byte[] bytes = _server.EndReceive(result, ref ep);
                Task.Run(() => ProcessPacket(bytes));
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
            finally
            {
                if ((!_stop) && (!_disposed))
                    _server.BeginReceive(new AsyncCallback(OnReceive), null);
            }
        }

        /// <summary>
        /// Processes incoming packet.
        /// </summary>
        private void ProcessPacket(byte[] bytes)
        {
            try
            {
                Packet p = Packet.FromBytes(bytes);
                if ((p != null) && (!String.IsNullOrWhiteSpace(p.SourceIP)))
                {
                    if (p.SourceIP == Config.LocalIP)
                        return;
                    NetworkDiscovery.AddOrUpdateRemoteInstance(p.SourceIP, p.Initials);                    
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }
    
    }
}

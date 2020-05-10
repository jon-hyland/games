using Common.Standard.Error;
using Common.Standard.Game;
using Common.Standard.Networking.Packets;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Common.Windows.Networking.Game.Discovery
{
    /// <summary>
    /// Receives discovery packets over UDP broadcast, for use in two-player lobby.
    /// </summary>
    public class DiscoveryServer : IDisposable
    {
        //private
        private readonly IErrorHandler _errorHandler = null;
        private readonly UdpClient _server = null;
        private readonly int _port = 0;
        private bool _stop = false;

        //events
        public event Action<Player> PlayerAnnounced;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public DiscoveryServer(int port, IErrorHandler errorHandler = null)
        {
            _errorHandler = errorHandler;
            _server = new UdpClient();
            _port = port;
            _stop = true;
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            _server?.Dispose();
        }

        /// <summary>
        /// Starts broadcasting.
        /// </summary>
        public void Start()
        {
            _stop = false;
            _server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _server.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
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
                _errorHandler?.LogError(ex);
            }
            finally
            {
                if (!_stop)
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
                DiscoveryPacket packet = (DiscoveryPacket)PacketBase.FromBytes(bytes);
                if (packet != null)
                {
                    Player player = Player.FromPacket(packet);
                    PlayerAnnounced?.Invoke(player);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
            }
        }


    }
}

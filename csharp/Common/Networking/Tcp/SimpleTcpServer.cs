using Common.Error;
using Common.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Common.Networking.Tcp
{
    public class SimpleTcpServer : IDisposable
    {
        //private
        private readonly IErrorHandler _errorHandler = null;
        private readonly ILogger _logger = null;
        private readonly TcpListener _listener;
        private Thread _listenThread = null;
        private readonly int _packetID = 0;
        private readonly int _timeoutMs = 1000;
        private SimpleTcpClient _lastConnectedClient = null;
        private bool _isListening => _listener?.Server?.Connected ?? false;

        //events
        public event Action<SimpleTcpClient> ClientConnected;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public SimpleTcpServer(string localIPAddress, int localPort, int packetID, int timeoutMs, IErrorHandler errorHandler = null, ILogger logger = null)
        {
            _errorHandler = errorHandler;
            _logger = logger;
            _listener = new TcpListener(IPAddress.Parse(localIPAddress), localPort);
            _packetID = packetID;
            _timeoutMs = timeoutMs;
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            _listenThread?.Abort();
            _listener.Stop();
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            _listener.Start();
            _listenThread = new Thread(ListenThread)
            {
                IsBackground = true
            };
            _listenThread.Start();
        }

        /// <summary>
        /// Listens for new TCP client connections.
        /// </summary>
        private void ListenThread()
        {
            while (true)
            {
                try
                {
                    TcpClient tcpClient = _listener.AcceptTcpClient();
                    SimpleTcpClient client = new SimpleTcpClient(tcpClient, _packetID, _timeoutMs, _errorHandler, _logger);
                    _lastConnectedClient = client;
                    ClientConnected?.Invoke(client);
                }
                catch (Exception ex)
                {
                    _errorHandler?.LogError(ex);
                }
            }
        }

        /// <summary>
        /// Waits for next connected client, or null if timeout reached.
        /// </summary>
        public SimpleTcpClient WaitForClient(int? timeoutMs = null)
        {
            DateTime start = DateTime.Now;
            while (true)
            {
                SimpleTcpClient client = _lastConnectedClient;
                if (client != null)
                {
                    _lastConnectedClient = null;
                    return client;
                }
                if ((timeoutMs != null) && ((DateTime.Now - start).TotalMilliseconds > timeoutMs))
                    return null;
                if (!_isListening)
                    return null;
                Thread.Sleep(2);
            }
        }



    }
}

using Common.Error;
using Common.Threading;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Common.Networking.Tcp
{
    /// <summary>
    /// Basic TCP client
    /// </summary>
    public class TcpClient
    {
        //private
        private IErrorHandler _errorHandler = null;
        private readonly string _ipAddress = "";
        private readonly int _port = 0;
        private readonly int _header = 0;
        private readonly byte[] _headerBytes = new byte[4];
        private readonly int _timeoutMs = 1000;
        private readonly List<byte> _incomingBuffer = new List<byte>();
        private readonly object _socketLock = new object();
        private Socket _socket = null;
        private bool _isConnected => _socket?.Connected ?? false;
        private long _socketErrors = 0;
        private readonly SimpleTimer _timer = null;
        private DateTime _lastPacketReceived_Time = DateTime.MaxValue;
        private TimeSpan _lastPacketReceived_Elapsed => DateTime.Now.Subtract(_lastPacketReceived_Time);

        //public
        public bool IsConnected => _isConnected;
        public long SocketErrors => _socketErrors;

        //events
        public event Action PacketReceived;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public TcpClient(string ipAddress, int port, int header, int timeoutMs, IErrorHandler errorHandler = null)
        {
            _errorHandler = errorHandler;
            _ipAddress = ipAddress;
            _port = port;
            _header = header;
            _headerBytes = BitConverter.GetBytes(_header);
            _timeoutMs = timeoutMs;
            _timer = new SimpleTimer(ReceiveTimer_Callback, 5);
        }

        /// <summary>
        /// Connects to configured TCP endpoint.
        /// </summary>
        public bool Connect()
        {
            return false;
        }












        /// <summary>
        /// Forces incoming buffer to be dealt with, even if no new data on the wire.
        /// In case of invalid data it needs to eventually flush it and move on.
        /// </summary>
        private void ReceiveTimer_Callback()
        {
            try
            {
                if (!_isConnected)
                    return;

                lock (_incomingBuffer)
                {
                    if ((_incomingBuffer.Count > 0) && (_lastPacketReceived_Elapsed.TotalMilliseconds >= 100))
                    {
                        //FixIncomingBuffer();
                        //ProcessIncomingBuffer();
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
            }
        }

    }
}

using Common.Error;
using Common.Extensions;
using Common.Logging;
using Common.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Common.Networking.Tcp
{
    /// <summary>
    /// A simple TCP client that connects to a server endpoint managed by its SimpleTcpServer pair class.
    /// Each packet sent or received must contain a predefined four-byte Int32 ID, unique to the application, 
    /// or it will be discarded.  Each packet will then contain a two-byte UInt16, identifying the total packet 
    /// length in bytes (including header & length bytes).
    /// </summary>
    public sealed class SimpleTcpClient : IDisposable
    {
        //private
        private readonly IErrorHandler _errorHandler = null;
        private readonly ILogger _logger = null;
        private bool _serverSide = false;
        private readonly IPAddress _serverIPAddress = null;
        private readonly int _serverPort = 0;
        private readonly IPAddress _clientIPAddress = null;
        private readonly int _packetID = 0;
        private readonly int _timeoutMs = 1000;
        private readonly List<byte> _incomingBuffer = new List<byte>();
        private readonly Queue<byte[]> _incomingApplicationPackets = new Queue<byte[]>();
        private readonly object _socketLock = new object();
        private Socket _socket = null;
        private bool _isConnected => _socket?.Connected ?? false;
        private long _socketErrors = 0;
        private readonly SimpleTimer _healthTimer = null;
        private DateTime _lastPacketReceived_Time = DateTime.MaxValue;
        private TimeSpan _lastPacketReceived_Elapsed => DateTime.Now.Subtract(_lastPacketReceived_Time);
        private readonly Queue<byte[]> _outgoingApplicationPackets = new Queue<byte[]>();
        private readonly ManualResetEventSlim _outgoingPacketSignal = new ManualResetEventSlim();
        private Thread _sendThread = null;
        private readonly ManualResetEventSlim _sendThreadSignal = new ManualResetEventSlim();

        //public
        public bool IsConnected => _isConnected;
        public long SocketErrors => _socketErrors;
        public bool ServerSide => _serverSide;

        //events
        public event Action PacketReceived;

        /// <summary>
        /// Class constructor (client side).
        /// </summary>
        public SimpleTcpClient(string serverIPAddress, int serverPort, int packetID, int timeoutMs, IErrorHandler errorHandler = null, ILogger logger = null)
        {
            _errorHandler = errorHandler;
            _logger = logger;
            _serverSide = false;
            _serverIPAddress = IPAddress.Parse(serverIPAddress);
            _serverPort = serverPort;
            _clientIPAddress = null;
            _packetID = packetID;
            _timeoutMs = timeoutMs;
            _healthTimer = new SimpleTimer(HealthTimer_Callback, 100);
            _sendThread = new Thread(SendThread)
            {
                IsBackground = true
            };
            _sendThread.Start();
        }

        /// <summary>
        /// Class constructor (server side).
        /// </summary>
        public SimpleTcpClient(string clientIPAddress, int packetID, int timeoutMs, IErrorHandler errorHandler = null, ILogger logger = null)
        {
            _errorHandler = errorHandler;
            _logger = logger;
            _serverSide = true;
            _serverIPAddress = null;
            _serverPort = 0;
            _clientIPAddress = IPAddress.Parse(clientIPAddress);
            _packetID = packetID;
            _timeoutMs = timeoutMs;
            _healthTimer = new SimpleTimer(HealthTimer_Callback, 100);
            _sendThread = new Thread(SendThread)
            {
                IsBackground = true
            };
            _sendThread.Start();
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            _healthTimer?.Dispose();
            _sendThread?.Abort();
            _socket?.Dispose();
        }

        #region Connect / Close

        /// <summary>
        /// Connects to configured TCP endpoint.
        /// </summary>
        public void Connect()
        {
            Stopwatch sw = Stopwatch.StartNew();
            bool success = false;

            try
            {
                //validate
                if (_serverSide)
                    throw new Exception("Cannot connect from server side");
                
                //lock socket
                lock (_socketLock)
                {
                    //message
                    WriteLog($"Connect [Start]");

                    try
                    {
                        //close existing socket?
                        if (_socket != null)
                        {
                            if (_socket.Connected)
                            {
                                WriteLog($"Connect [Closing existing socket]");
                                _socket.Close();
                            }
                            _socket.Dispose();
                        }
                    }
                    catch
                    {
                    }

                    //clear incoming buffer
                    lock (_incomingBuffer)
                    {
                        _incomingBuffer.Clear();
                    }

                    //create socket
                    WriteLog($"Connect [Creating socket]");
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true,
                        SendTimeout = _timeoutMs
                    };

                    //open socket
                    WriteLog($"Connect [Connecting to server endpoint [Address: {_serverIPAddress.ToString()}, Port: {_serverPort}, Timeout_Ms: {_timeoutMs}]]");
                    _socket.Connect(_serverIPAddress, _serverPort, TimeSpan.FromMilliseconds(_timeoutMs));

                    //reset error count
                    _socketErrors = 0;

                    //begin receiving next response packet
                    BeginReceive(_socket);

                    //success
                    success = true;
                }
            }
            catch (Exception ex)
            {
                _socketErrors++;
                _errorHandler?.LogError(ex);
                throw;
            }
            finally
            {
                WriteLog($"Connect [End [Success: {(success ? "1" : "0")}, Elapsed_Ms: {sw.ElapsedMilliseconds}]]");
            }
        }

        /// <summary>
        /// Closes connection to TCP endpoint.
        /// </summary>
        public void Close()
        {
            bool success = false;

            try
            {
                //lock socket
                lock (_socketLock)
                {
                    //message
                    WriteLog($"Close [Start]");

                    //close socket
                    if (_socket != null)
                    {
                        if (_socket.Connected)
                        {
                            WriteLog($"Connect [Closing existing socket]");
                            _socket.Close();
                        }
                        _socket.Dispose();
                        _socket = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _socketErrors++;
                _errorHandler?.LogError(ex);
                throw;
            }
            finally
            {
                WriteLog($"Close [End [Success: {(success ? "1" : "0")}]]");
            }
        }

        #endregion

        #region Send

        /// <summary>
        /// Adds packet to the send queue, and signals send thread of data.
        /// </summary>
        public void SendPacket(byte[] payload)
        {
            WriteLog($"SendPacket [Queuing {payload} payload bytes]");
            lock (_outgoingApplicationPackets)
            {
                _outgoingApplicationPackets.Enqueue(payload);
                _outgoingPacketSignal.Set();
            }
        }

        /// <summary>
        /// Primary thread to handle sending outgoing packets.
        /// </summary>
        private void SendThread()
        {
            //set signal
            _sendThreadSignal.Set();

            //loop forever
            while (true)
            {
                try
                {
                    //wait for packet to send
                    _outgoingPacketSignal.Wait(15);

                    //send packets
                    lock (_outgoingApplicationPackets)
                    {
                        while (_outgoingApplicationPackets.Count > 0)
                        {
                            byte[] payloadBytes = _outgoingApplicationPackets.Dequeue();
                            byte[] packetBytes = new byte[payloadBytes.Length + 6];
                            byte[] idBytes = BitConverter.GetBytes(_packetID);
                            Array.Copy(idBytes, 0, packetBytes, 0, 4);
                            byte[] lengthBytes = BitConverter.GetBytes((ushort)packetBytes.Length);
                            Array.Copy(lengthBytes, 0, packetBytes, 4, 2);

                            if (_isConnected)
                            {
                                _socket.Send(packetBytes);
                                WriteLog($"SendThread [Sent {packetBytes} packet bytes]");
                            }
                        }
                        _outgoingPacketSignal.Reset();
                    }                
                }
                catch (Exception ex)
                {
                    _errorHandler?.LogError(ex);
                }
            }
        }

        #endregion

        #region Receive

        /// <summary>
        /// Returns next received packet, or null if no more data.
        /// </summary>
        public byte[] GetIncomingPacket()
        {
            lock (_incomingApplicationPackets)
            {
                if (_incomingApplicationPackets.Count > 0)
                {
                    byte[] payload = _incomingApplicationPackets.Dequeue();
                    return payload;
                }
                return null;
            }
        }

        /// <summary>
        /// Begins waiting for first response data, async.
        /// </summary>
        private void BeginReceive(Socket socket)
        {
            try
            {
                //state
                ReceiveState state = new ReceiveState
                {
                    ClientSocket = socket
                };

                //begin receive
                WriteLog($"BeginReceive [BeginReceive]");
                socket.BeginReceive(state.Buffer, 0, ReceiveState.BUFFER_SIZE, 0, new AsyncCallback(ReceiveData), state);
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
                _socketErrors++;
                //if (!socket.Connected)  //needed?
                //    Connect();
            }
        }

        /// <summary>
        /// Fired when there's data on the receive wire.  Usually its one complete application packet.  
        /// Sometimes it might *not* be complete packet.  Sometimes it might be *more* than one packet.
        /// Either way, add to incoming buffer to be evaluated and processed.
        /// </summary>
        private void ReceiveData(IAsyncResult ar)
        {
            ReceiveState state = (ReceiveState)ar.AsyncState;
            Socket socket = state.ClientSocket;

            try
            {
                //set flag just in case (not sure if needed)
                socket.NoDelay = true;

                //end receive
                WriteLog($"ReceiveData [EndReceive]");
                int bytesRead = socket.EndReceive(ar);

                try
                {
                    //lock buffer
                    lock (_incomingBuffer)
                    {
                        //dump on overflow?
                        if (_incomingBuffer.Count >= 8192)
                        {
                            WriteLog("ReceiveData [Buffer overflow, dumping data!]");
                            _incomingBuffer.Clear();
                        }

                        //read data into buffer
                        int beforeLength = _incomingBuffer.Count;
                        byte[] bytes = new byte[bytesRead];
                        Array.Copy(state.Buffer, 0, bytes, 0, bytesRead);
                        _incomingBuffer.AddRange(bytes);
                        WriteLog($"ReceiveData [Bytes read from socket [Bytes_Received: {bytesRead}, Buffer_Length_Before: {beforeLength}, Buffer_Length_After: {_incomingBuffer.Count}]");

                        //process buffer
                        ProcessIncomingBuffer();
                    }
                }
                finally
                {
                    //begin receive
                    if (socket != null)
                    {
                        WriteLog($"ReceiveData [BeginReceive]");
                        socket.BeginReceive(state.Buffer, 0, ReceiveState.BUFFER_SIZE, 0, new AsyncCallback(ReceiveData), state);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
            }
        }

        /// <summary>
        /// Processes the incoming byte buffer.  Turn this into zero, one, or more application packets.
        /// </summary>
        private void ProcessIncomingBuffer()
        {
            bool fireEvent = false;

            try
            {
                //lock buffer
                lock (_incomingBuffer)
                {
                    //loop while data in buffer, or we break out on purpose (limit to 1000 cycles)
                    for (int i = 0; i < 1000; i++)
                    {
                        //return if no data
                        if (_incomingBuffer.Count == 0)
                            return;

                        try
                        {
                            //message
                            WriteLog($"ProcessIncomingBuffer [Start]");

                            //find first instance of expected id (four unique bytes).
                            //should be first data in buffer (if not there's problem!) but log and recover anyway                            
                            int startIndex = FindPacketID(_incomingBuffer);
                            if (startIndex == 0)
                            {
                                WriteLog($"ProcessIncomingBuffer [Found ID at buffer index {startIndex}]");
                            }
                            if (startIndex == -1)
                            {
                                WriteLog($"ProcessIncomingBuffer [ID not found in buffer!]");
                                return;
                            }

                            //not first data in buffer?
                            if (startIndex > 0)
                            {
                                WriteLog($"ProcessIncomingBuffer [ID index {startIndex} not expected!]");
                                WriteLog($"ProcessIncomingBuffer [Discarding {startIndex} bytes in order to recover!]");
                                _incomingBuffer.RemoveRange(0, startIndex);
                            }

                            //not enough data to read header? (fix this, no longer valid)
                            if (_incomingBuffer.Count < 6)
                            {
                                WriteLog($"ProcessIncomingBuffer [Buffer length is {_incomingBuffer.Count}, data incomplete, unable to continue!]");
                                return;
                            }

                            //read header values
                            byte[] bufferBytes = _incomingBuffer.ToArray();
                            int id = BitConverter.ToInt32(bufferBytes, 0);
                            ushort length = BitConverter.ToUInt16(bufferBytes, 4);

                            //validate
                            if (id != _packetID)
                            {
                                WriteLog($"ProcessIncomingBuffer [Packet ID {id} is invalid!]");
                                return;
                            }
                            if (bufferBytes.Length < length)
                            {
                                WriteLog($"ProcessIncomingBuffer [Packet incomplete or length wrong! [Advertised_Length: {length}, Actual_Length: {bufferBytes.Length}]]");
                                return;
                            }

                            //read packet bytes, remove from buffer
                            WriteLog($"ProcessIncomingBuffer [Removing {length} bytes from buffer]");
                            byte[] packetBytes = _incomingBuffer.Take(length).ToArray();
                            _incomingBuffer.RemoveRange(0, length);
                            _lastPacketReceived_Time = DateTime.Now;

                            //create packet
                            WriteLog($"ProcessIncomingBuffer [Creating application packet]");
                            byte[] payloadBytes = new byte[packetBytes.Length - 6];
                            Array.Copy(packetBytes, 6, payloadBytes, 0, payloadBytes.Length);
                            lock (_incomingApplicationPackets)
                            {
                                _incomingApplicationPackets.Enqueue(payloadBytes);
                            }
                            fireEvent = true;
                        }
                        finally
                        {
                            //message
                            WriteLog($"ProcessIncomingBuffer [End]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
            }
            finally
            {
                try
                {
                    //fire event?
                    if (fireEvent)
                        PacketReceived?.Invoke();
                }
                catch (Exception ex)
                {
                    _errorHandler?.LogError(ex);
                }
            }
        }

        /// <summary>
        /// Fixes the incoming buffer.  Salvages what it can, discards the rest.
        /// Not sure if this is needed anymore.. was ported from older business code and was used
        /// to fix TCP transmission errors from a certain device with a faulty firmware version.
        /// </summary>
        private void FixIncomingBuffer()
        {
            lock (_incomingBuffer)
            {
                //return if no data
                if (_incomingBuffer.Count == 0)
                    return;

                try
                {
                    //message
                    WriteLog($"FixIncomingBuffer [Start]");

                    //discard everything if less than 6 bytes
                    if (_incomingBuffer.Count < 6)
                    {
                        WriteLog($"FixIncomingBuffer [Buffer contains less than 6 bytes]");
                        WriteLog($"FixIncomingBuffer [Discarding entire buffer ({_incomingBuffer.Count} bytes) in order to recover!]");
                        _incomingBuffer.Clear();
                        return;
                    }

                    //discard everything if no id found
                    int startIndex = FindPacketID(_incomingBuffer);
                    if (startIndex == -1)
                    {
                        WriteLog($"FixIncomingBuffer [Packet ID not found in incoming buffer]");
                        WriteLog($"FixIncomingBuffer [Discarding entire buffer ({_incomingBuffer.Count} bytes) in order to recover!]");
                        _incomingBuffer.Clear();
                        return;
                    }

                    //discard partial if id not first
                    if (startIndex > 0)
                    {
                        WriteLog($"FixIncomingBuffer [Packet ID not first data in buffer]");
                        WriteLog($"FixIncomingBuffer [Discarding first {startIndex} bytes in order to recover!]");
                        _incomingBuffer.RemoveRange(0, startIndex);
                    }

                    //discard everything if less than 6 bytes (again)
                    if (_incomingBuffer.Count < 6)
                    {
                        WriteLog($"FixIncomingBuffer [Buffer contains less than 6 bytes]");
                        WriteLog($"FixIncomingBuffer [Discarding entire buffer ({_incomingBuffer.Count} bytes) in order to recover!]");
                        _incomingBuffer.Clear();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _errorHandler?.LogError(ex);
                    WriteLog($"FixIncomingBuffer [Unexpected error]");
                    WriteLog($"FixIncomingBuffer [Discarding entire buffer ({_incomingBuffer.Count} bytes) in order to recover!]");
                    _incomingBuffer.Clear();
                }
                finally
                {
                    //set flag to prevent immediate reentry
                    _lastPacketReceived_Time = DateTime.Now;

                    //message
                    WriteLog($"FixIncomingBuffer [End]");
                }
            }
        }

        /// <summary>
        /// Scans through buffer looking for sequence of 4 sequential bytes.
        /// Returns index of first byte if all present (should be 0 unless buffer problem).
        /// Returns -1 if pattern not found.
        /// </summary>
        private int FindPacketID(List<byte> buffer, int startIndex = 0)
        {
            byte[] idBytes = BitConverter.GetBytes(_packetID);
            for (int i = startIndex; i < buffer.Count - 3; i++)
            {
                if ((buffer[i] == idBytes[0])
                    && (buffer[i + 1] == idBytes[1])
                    && (buffer[i + 2] == idBytes[2])
                    && (buffer[i + 3] == idBytes[3]))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Forces incoming buffer to be dealt with, even if no new data on the wire.
        /// In case of invalid data it needs to eventually flush it and move on.
        /// </summary>
        private void HealthTimer_Callback()
        {
            try
            {
                //return if socket not connected
                if (!_isConnected)
                    return;

                //restart send thread if it died
                if ((_sendThreadSignal.IsSet) && ((_sendThread == null) || (!_sendThread.IsAlive)))
                {
                    WriteLog($"HealthTimer [Send thread died and was restarted]");
                    _sendThreadSignal.Reset();
                    _sendThread = new Thread(SendThread)
                    {
                        IsBackground = true
                    };
                    _sendThread.Start();
                }

                //fix incoming buffer if problem
                lock (_incomingBuffer)
                {
                    if ((_incomingBuffer.Count > 0) && (_lastPacketReceived_Elapsed.TotalMilliseconds >= 1000))
                    {
                        FixIncomingBuffer();
                        ProcessIncomingBuffer();
                    }
                }

                //fire event (again?) if packets still in incoming queue
                bool fireEvent = false;
                lock (_incomingApplicationPackets)
                {
                    if (_incomingApplicationPackets.Count > 0)
                        fireEvent = true;
                }
                if (fireEvent)
                    PacketReceived?.Invoke();
            }
            catch (Exception ex)
            {
                _errorHandler?.LogError(ex);
            }
        }

        #endregion

        #region Logging

        /// <summary>
        /// Writes a log message using class defaults.
        /// </summary>
        private void WriteLog(string message)
        {
            _logger?.Write(LogLevel.Medium, "TcpClient", message);
        }

        #endregion

    }
}

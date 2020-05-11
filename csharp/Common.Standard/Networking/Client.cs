using Common.Standard.Error;
using Common.Standard.Extensions;
using Common.Standard.Logging;
using Common.Standard.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Common.Standard.Networking
{
    public class Client : IClient, IDisposable
    {
        //private
        private readonly TcpClient _client;
        private readonly IPEndPoint _endpoint;
        private readonly List<byte> _incomingQueue;
        private readonly List<byte> _outgoingQueue;
        private readonly Thread _readThread;
        private readonly Thread _writeThread;
        private readonly ManualResetEventSlim _readSignal;
        private readonly ManualResetEventSlim _writeSignal;
        private bool _stop;

        //public
        public IPAddress RemoteIP => ((IPEndPoint)_client.Client.RemoteEndPoint).Address;
        public bool IsConnected => _client.Connected;

        //events
        public event Action<Client, PacketBase> PacketReceived;

        /// <summary>
        /// Class constructor (standalone / client side).
        /// </summary>
        public Client(IPAddress ip, int port)
        {
            _client = new TcpClient();
            _endpoint = new IPEndPoint(ip, port);
            _incomingQueue = new List<byte>();
            _outgoingQueue = new List<byte>();
            _readThread = new Thread(Read_Thread);
            _readThread.IsBackground = true;
            _writeThread = new Thread(Write_Thread);
            _writeThread.IsBackground = true;
            _readSignal = new ManualResetEventSlim();
            _writeSignal = new ManualResetEventSlim();
        }

        /// <summary>
        /// Class constructor (server side, from listener).
        /// </summary>
        public Client(TcpClient client)
        {
            _client = client;
            _endpoint = null;
            _incomingQueue = new List<byte>();
            _outgoingQueue = new List<byte>();
            _readThread = new Thread(Read_Thread);
            _readThread.IsBackground = true;
            _writeThread = new Thread(Write_Thread);
            _writeThread.IsBackground = true;
            _readSignal = new ManualResetEventSlim();
            _writeSignal = new ManualResetEventSlim();
            _readThread.Start();
            _writeThread.Start();
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            _stop = true;
            _client?.Dispose();
        }

        /// <summary>
        /// Connects to configured endpoint (standalone / client side).
        /// </summary>
        public bool Connect(int timeoutMs = 2500)
        {
            try
            {
                _client.Connect(_endpoint.Address, _endpoint.Port, TimeSpan.FromMilliseconds(timeoutMs));
                _readThread.Start();
                _writeThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// Serializes packet and adds to outgoing queue.
        /// </summary>
        public void SendPacket(PacketBase packet)
        {
            lock (_outgoingQueue)
            {
                _outgoingQueue.AddRange(packet.ToBytes());
                _writeSignal.Set();
            }
        }

        /// <summary>
        /// Loops forever, reading data from client.
        /// Fires event for each complete game packet received.
        /// </summary>
        private void Read_Thread()
        {
            //async callback loop, reading data into incoming queue
            byte[] buffer = new byte[8192];
            NetworkStream stream = _client.GetStream();
            void callback(IAsyncResult ar)
            {
                int bytesRead = stream.EndRead(ar);
                Log.Write($"ReadData: {bytesRead} bytes read");
                for (int i = 0; i < bytesRead; i++)
                    _incomingQueue.Add(buffer[i]);
                if (_stop)
                    return;
                stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(callback), null);
            }
            stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(callback), null);

            //loop forever, pulling complete packets out of incoming queue
            while (true)
            {
                //wait new data in queue, or one second
                _readSignal.Wait(1000);
                _readSignal.Reset();

                //read data, if it exists
                ReadData();

                //end thread?
                if (_stop)
                    break;
            }
        }

        /// <summary>
        /// Reads data from incoming stream (if bytes exist in buffer).  Tries to construct 
        /// one or more game packets. Fires event for each valid game packet.
        /// </summary>
        private void ReadData()
        {
            try
            {
                //vars
                List<byte[]> packets = new List<byte[]>();

                //lock queue
                lock (_incomingQueue)
                {
                    //return if zero bytes
                    if (_incomingQueue.Count == 0)
                        return;

                    //queue overflow?
                    if (_incomingQueue.Count > 100000)
                    {
                        Log.Write($"ReadData: Incoming byte queue overflow");
                        _incomingQueue.Clear();
                    }

                    //loop
                    while (true)
                    {
                        //break if zero bytes
                        if (_incomingQueue.Count == 0)
                            break;

                        //find first four matching footer bytes (terminator)
                        int firstIndex = FindToken(_incomingQueue, PacketBase.PACKET_FOOTER);

                        //break if no footer
                        if (firstIndex == -1)
                        {
                            Log.Write($"ReadData: Incomplete data ({_incomingQueue.Count} bytes) left in buffer");
                            break;
                        }

                        //dequeue bytes
                        int count = firstIndex + 4;
                        byte[] bytes = _incomingQueue.Dequeue(count).ToArray();

                        //add to list
                        packets.Add(bytes);
                    }
                }

                //return if no new packets
                if (packets.Count <= 0)
                    return;

                //message
                Log.Write($"ReadData: {packets.Count} packets read in one pass");

                //loop through packet (candidates)
                foreach (byte[] bytes in packets)
                {
                    //reject if invalid
                    PacketBase packet = PacketBase.FromBytes(bytes);
                    if (packet == null)
                    {
                        Log.Write("ReadData: Invalid packet was discarded");
                        continue;
                    }

                    //fire event
                    PacketReceived?.Invoke(this, packet);
                }
            }
            catch (Exception ex)
            {
                Log.Write("ReadData: Reading or processing data");
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Loops forever, writing data to client.
        /// </summary>
        private void Write_Thread()
        {
            //get stream
            NetworkStream stream = _client.GetStream();

            //loop forever
            while (true)
            {
                //wait for data to send, or one second
                _writeSignal.Wait(1000);
                _writeSignal.Reset();

                //write data, if it exists
                WriteData(stream);

                //end thread?
                if (_stop)
                    break;
            }
        }



        /// <summary>
        /// Writes any pending outgoing data to network stream.
        /// </summary>
        private void WriteData(NetworkStream stream)
        {
            try
            {
                //lock queue
                lock (_outgoingQueue)
                {
                    //have data to send?
                    if (_outgoingQueue.Count > 0)
                    {
                        //convert to array
                        byte[] buffer = _outgoingQueue.ToArray();

                        //clear queue
                        _outgoingQueue.Clear();

                        //write array
                        stream.Write(buffer, 0, buffer.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("WriteData: Error writing outgoing data");
                ErrorHandler.LogError(ex);
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


    }
}

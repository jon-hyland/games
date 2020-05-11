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
        private readonly Thread _thread;
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
            _thread = new Thread(IO_Thread)
            {
                IsBackground = true
            };
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
            _thread = new Thread(IO_Thread)
            {
                IsBackground = true
            };
            _thread.Start();
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
                _thread.Start();
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
            }
        }

        /// <summary>
        /// Loops forever, reading and writing data to client.
        /// Fires event for each complete game packet received.
        /// </summary>
        private void IO_Thread()
        {
            NetworkStream stream = _client.GetStream();
            while (true)
            {
                ReadData(stream);
                WriteData(stream);
                Thread.Sleep(2);
                if (_stop)
                    break;
            }
        }

        /// <summary>
        /// Reads data from incoming stream (if bytes exist in buffer).  Tries to construct 
        /// one or more game packets. Fires event for each valid game packet.
        /// </summary>
        private void ReadData(NetworkStream stream)
        {
            try
            {
                //vars
                List<byte[]> packets = new List<byte[]>();

                //lock queue
                lock (_incomingQueue)
                {
                    //queue overflow?
                    if (_incomingQueue.Count > 1000000)
                        _incomingQueue.Clear();

                    //read until no more data
                    byte[] buffer = new byte[8192];
                    while (stream.DataAvailable)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);
                        for (int i = 0; i < read; i++)
                            _incomingQueue.Add(buffer[i]);
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
                            Log.Write($"IncomingData: Incomplete data ({_incomingQueue.Count} bytes) left in buffer");
                            break;
                        }

                        //dequeue bytes
                        int count = firstIndex + 4;
                        byte[] bytes = _incomingQueue.Dequeue(count).ToArray();

                        //add to list
                        packets.Add(bytes);
                    }
                }

                //message
                if (packets.Count > 1)
                    Log.Write($"IncomingData: {packets.Count} packets read in one pass");

                //loop through packet (candidates)
                foreach (byte[] bytes in packets)
                {
                    //reject if invalid
                    PacketBase packet = PacketBase.FromBytes(bytes);
                    if (packet == null)
                    {
                        Log.Write("IncomingData: Invalid packet was discarded");
                        continue;
                    }

                    //fire event
                    PacketReceived?.Invoke(this, packet);
                }
            }
            catch (Exception ex)
            {
                Log.Write("WriteData: Reading or processing data");
                ErrorHandler.LogError(ex);
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

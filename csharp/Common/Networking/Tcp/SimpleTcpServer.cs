using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Common.Networking.Tcp
{
    public class SimpleTcpServer
    {
        private TcpListener _listener;
        private HashSet<string> _allowedClientIPs = new HashSet<string>();
        private Thread _listenThread = null;

        public event Action<TcpClient> ClientConnected;

        public SimpleTcpServer(string localIPAddress, int localPort, IEnumerable<string> allowedClientIPs = null)
        {
            _listener = new TcpListener(IPAddress.Parse(localIPAddress), localPort);
            
            foreach (string ip in allowedClientIPs)
                if (!_allowedClientIPs.Contains(ip))
                    _allowedClientIPs.Add(ip);
        }

        public void Start()
        {
            _listener.Start();
            _listenThread = new Thread(ListenThread)
            {
                IsBackground = true
            };
            _listenThread.Start();
        }

        private void ListenThread()
        {
            while (true)
            {
                TcpClient client = _listener.AcceptTcpClient();
                string clientIP = (client.Client.RemoteEndPoint as IPEndPoint).ToString();
                if ((_allowedClientIPs.Count > 0) && (!_allowedClientIPs.Contains(clientIP)))
                {
                    client.Close();
                    return;
                }
                ClientConnected?.Invoke(client);
            }
        }



    }
}

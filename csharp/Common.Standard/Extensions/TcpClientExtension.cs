using System;
using System.Net;
using System.Net.Sockets;

namespace Common.Standard.Extensions
{
    public static class TcpClientExtension
    {
        /// <summary>
        /// Connect with timeout.
        /// </summary>
        public static void Connect(this TcpClient client, IPAddress address, int port, TimeSpan timeout)
        {
            IAsyncResult result = client.BeginConnect(address, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
            {
                client.EndConnect(result);
            }
            else
            {
                client.Close();
                throw new SocketException(10060);
            }
        }

        /// <summary>
        /// Connect with timeout.
        /// </summary>
        public static void Connect(this TcpClient client, IPAddress[] addresses, int port, TimeSpan timeout)
        {
            IAsyncResult result = client.BeginConnect(addresses, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
            {
                client.EndConnect(result);
            }
            else
            {
                client.Close();
                throw new SocketException(10060);
            }
        }

        /// <summary>
        /// Connect with timeout.
        /// </summary>
        public static void Connect(this TcpClient client, string host, int port, TimeSpan timeout)
        {
            IAsyncResult result = client.BeginConnect(host, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
            {
                client.EndConnect(result);
            }
            else
            {
                client.Close();
                throw new SocketException(10060);
            }
        }
    }
}

using System;
using System.Net;
using System.Net.Sockets;

namespace Common.Standard.Extensions
{
    public static class SocketExtension
    {
        /// <summary>
        /// Connect with timeout.
        /// </summary>
        public static void Connect(this Socket socket, EndPoint endpoint, TimeSpan timeout)
        {
            IAsyncResult result = socket.BeginConnect(endpoint, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
            {
                socket.EndConnect(result);
            }
            else
            {
                socket.Close();
                throw new SocketException(10060);
            }
        }

        /// <summary>
        /// Connect with timeout.
        /// </summary>
        public static void Connect(this Socket socket, IPAddress address, int port, TimeSpan timeout)
        {
            IAsyncResult result = socket.BeginConnect(address, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
            {
                socket.EndConnect(result);
            }
            else
            {
                socket.Close();
                throw new SocketException(10060);
            }
        }

        /// <summary>
        /// Connect with timeout.
        /// </summary>
        public static void Connect(this Socket socket, IPAddress[] addresses, int port, TimeSpan timeout)
        {
            IAsyncResult result = socket.BeginConnect(addresses, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
            {
                socket.EndConnect(result);
            }
            else
            {
                socket.Close();
                throw new SocketException(10060);
            }
        }

        /// <summary>
        /// Connect with timeout.
        /// </summary>
        public static void Connect(this Socket socket, string host, int port, TimeSpan timeout)
        {
            IAsyncResult result = socket.BeginConnect(host, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
            {
                socket.EndConnect(result);
            }
            else
            {
                socket.Close();
                throw new SocketException(10060);
            }
        }

    }
}

using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Common.Standard.Networking
{
    public static class DnsHelper
    {
        /// <summary>
        /// Performs DNS lookup on hostname, returns IP address.
        /// </summary>
        public static IPAddress ResolveHost(string host)
        {
            IPAddress ip = null;
            try
            {
                ip = Dns.GetHostEntry(host).AddressList.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
            }
            catch
            {
            }
            return ip ?? IPAddress.None;
        }
    }
}

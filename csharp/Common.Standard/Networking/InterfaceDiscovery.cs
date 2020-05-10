using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Common.Standard.Networking
{
    /// <summary>
    /// Used for local network discovery.
    /// </summary>
    public static class InterfaceDiscovery
    {
        /// <summary>
        /// Discovers the more likely network interface, and returns its local IP.
        /// </summary>
        public static IPAddress GetLocalIP()
        {
            List<NetworkInterface> interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up)
                .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet || i.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet || i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .Where(i => i.GetIPProperties()?.GatewayAddresses.Count > 0)
                .ToList();

            List<IPAddress> ips = new List<IPAddress>();
            foreach (NetworkInterface i in interfaces)
            {
                IPInterfaceProperties p = i.GetIPProperties();
                IPAddress ipa = p.UnicastAddresses
                    .Select(ua => ua.Address)
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                    .FirstOrDefault();
                if (ipa != null)
                    ips.Add(ipa);
            }

            IPAddress ip = ips.FirstOrDefault();
            return ip;
        }



    }
}

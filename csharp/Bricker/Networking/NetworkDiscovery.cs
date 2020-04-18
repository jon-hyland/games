using Bricker.Configuration;
using Bricker.Error;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Bricker.Networking
{
    /// <summary>
    /// Used for local network discovery.
    /// </summary>
    public static class NetworkDiscovery
    {
        //private
        private static readonly List<RemoteInstance> _instances;

        //public
        public static int RemoteInstanceCount => _instances.Count;

        /// <summary>
        /// Static class constructor.
        /// </summary>
        static NetworkDiscovery()
        {
            _instances = LoadRemoteInstancesFromDisk();
        }
        
        /// <summary>
        /// Discovers the more likely network interface, and returns its local IP.
        /// </summary>
        public static string GetLocalIP()
        {
            List<NetworkInterface> interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up)
                .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet || i.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet || i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .Where(i => i.GetIPProperties()?.GatewayAddresses.Count > 0)
                .ToList();

            List<string> ips = new List<string>();
            foreach (NetworkInterface i in interfaces)
            {
                IPInterfaceProperties p = i.GetIPProperties();
                IPAddress ipa = p.UnicastAddresses
                    .Select(ua => ua.Address)
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                    .FirstOrDefault();
                if (ipa != null)
                    ips.Add(ipa.ToString());
            }

            string ip = ips.FirstOrDefault();
            return ip;
        }

        /// <summary>
        /// Gets shallow copy of remote instance list.
        /// </summary>
        public static List<RemoteInstance> GetRemoteInstances()
        {
            lock (_instances)
            {
                return _instances.ToList();
            }
        }

        /// <summary>
        /// Adds or updates a remote instance, if it has changed.
        /// </summary>
        public static void AddOrUpdateRemoteInstance(string ip, string initials)
        {
            lock (_instances)
            {
                RemoteInstance instance = _instances
                    .Where(i => i.IP == ip)
                    .FirstOrDefault();

                if (instance != null)
                {
                    if (instance.Initials != initials)
                        instance.Initials = initials;
                    instance.LastDiscovery = DateTime.Now;
                }
                else
                {
                    _instances.Add(new RemoteInstance(ip, initials, DateTime.Now));
                }
               
                List<RemoteInstance> instances = _instances
                    .OrderByDescending(i => i.LastDiscovery)
                    .Take(5)
                    .ToList();

                _instances.Clear();
                _instances.AddRange(instances);
                SaveRemoteInstances(instances);
            }
        }

        /// <summary>
        /// Saves remote instances list to disk.
        /// </summary>
        private static void SaveRemoteInstances(List<RemoteInstance> instances)
        {
            try
            {
                string text = String.Join(Environment.NewLine, instances.Select(i => $"{i.IP}|{i.Initials}|{i.LastDiscovery}"));
                File.WriteAllText(Config.RemoteInstanceFile, text);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Loads remote instances from disk.
        /// </summary>
        private static List<RemoteInstance> LoadRemoteInstancesFromDisk()
        {
            try
            {
                if (!File.Exists(Config.RemoteInstanceFile))
                    return new List<RemoteInstance>();

                List<RemoteInstance> instances = new List<RemoteInstance>();
                string[] lines = File.ReadAllText(Config.RemoteInstanceFile).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string[] split = line.Split('|');
                    if (split.Length != 3)
                        continue;

                    DateTime.TryParse(split[3], out DateTime last);                   
                    instances.Add(new RemoteInstance(split[0], split[1], last));
                }

                instances = instances
                    .OrderByDescending(i => i.LastDiscovery)
                    .Take(5)
                    .ToList();

                return instances;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
            return new List<RemoteInstance>();
        }        

    }
}

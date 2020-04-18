using System;

namespace Bricker.Networking
{
    /// <summary>
    /// Represents a discovered remote instance (opponent).
    /// </summary>
    public class RemoteInstance
    {
        public string IP { get; }
        public string Initials { get; set; }
        public DateTime LastDiscovery { get; set; }

        public RemoteInstance(string ip, string initials, DateTime lastDiscovery)
        {
            IP = ip;
            Initials = initials;
            LastDiscovery = lastDiscovery;
        }
    }
}

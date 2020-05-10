using System;
using System.Net;

namespace Common.Standard.Networking.Packets
{
    /// <summary>
    /// Represents a TCP packet used for endpoint heartbeats.
    /// </summary>
    public class HeartbeatPacket : PacketBase
    {
        //private
        private readonly long _count;

        //public
        public long Count => _count;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public HeartbeatPacket(string gameTitle, Version gameVersion, IPAddress sourceIP, IPAddress destinationIP,
            ushort destinationPort, string playerName, long count)
            : base(PacketType.Heartbeat, gameTitle, gameVersion, sourceIP, destinationIP, destinationPort, playerName)
        {
            _count = count;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public HeartbeatPacket(PacketParser parser)
            : base(PacketType.Heartbeat, parser)
        {
            _count = parser.GetInt64();
        }

        /// <summary>
        /// Adds instance bytes to packet builder.
        /// </summary>
        protected override void AddInstanceBytes(PacketBuilder builder)
        {
            builder.AddInt64(_count);
        }

    }
}

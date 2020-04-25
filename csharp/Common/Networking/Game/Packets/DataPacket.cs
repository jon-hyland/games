using System;
using System.Net;

namespace Common.Networking.Game.Packets
{
    /// <summary>
    /// Represents a TCP packet used to send generic one-way data.
    /// </summary>
    public class DataPacket : PacketBase
    {
        //private
        private readonly byte[] _data;

        //public
        public byte[] Data => _data;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public DataPacket(string gameTitle, Version gameVersion, IPAddress sourceIP, IPAddress destinationIP,
            ushort destinationPort, byte[] data)
            : base(PacketType.Data, gameTitle, gameVersion, sourceIP, destinationIP, destinationPort)
        {
            _data = data ?? new byte[0];
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public DataPacket(PacketParser parser)
            : base(PacketType.Data, parser)
        {
            _data = parser.GetBytes();
        }

        /// <summary>
        /// Adds instance bytes to packet builder.
        /// </summary>
        protected override void AddInstanceBytes(PacketBuilder builder)
        {
            builder.AddBytes(_data);
        }

    }
}

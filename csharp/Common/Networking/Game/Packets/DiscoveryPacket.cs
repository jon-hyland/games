using System;
using System.Net;

namespace Common.Networking.Game.Packets
{
    /// <summary>
    /// Represents a UDP packet used for opponent discovery.
    /// </summary>
    public sealed class DiscoveryPacket : PacketBase
    {
        //private
        private readonly IPAddress _playerIP;
        private readonly ushort _playerPort;
        private readonly string _playerName;

        //public
        public IPAddress PlayerIP => _playerIP;
        public ushort PlayerPort => _playerPort;
        public string PlayerName => _playerName;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public DiscoveryPacket(string gameTitle, Version gameVersion, IPAddress sourceIP, IPAddress destinationIP, 
            ushort destinationPort, IPAddress playerIP, ushort playerPort, string playerName)
            : base(PacketType.Discovery, gameTitle, gameVersion, sourceIP, destinationIP, destinationPort)
        {
            _playerIP = playerIP;
            _playerPort = playerPort;
            _playerName = playerName;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public DiscoveryPacket(PacketParser parser)
            : base(PacketType.Discovery, parser)
        {
            _playerIP = parser.GetIPAddress();
            _playerPort = parser.GetUInt16();
            _playerName = parser.GetString();
        }

        /// <summary>
        /// Adds instance bytes to packet builder.
        /// </summary>
        protected override void AddInstanceBytes(PacketBuilder builder)
        {
            builder.AddIPAddress(_playerIP);
            builder.AddUInt16(_playerPort);
            builder.AddString(_playerName);
        }
    }
}

using System;
using System.Net;

namespace Common.Networking.Game.Packets
{
    /// <summary>
    /// Base class for game packets.
    /// </summary>
    public abstract class PacketBase
    {
        //const
        public const int PACKET_HEADER = 1296911693;     // bytes 77,77,77,77
        public const int PACKET_FOOTER = 1448498774;     // bytes 86,86,86,86

        //private
        protected readonly PacketType _type;
        protected readonly string _gameTitle;
        protected readonly Version _gameVersion;
        protected readonly IPAddress _sourceIP;
        protected readonly IPAddress _destinationIP;
        protected readonly ushort _destinationPort;

        //public
        public string GameTitle => _gameTitle;
        public PacketType Type => _type;
        public Version GameVersion => _gameVersion;
        public IPAddress SourceIP => _sourceIP;
        public IPAddress DestinationIP => _destinationIP;
        public ushort DestinationPort => _destinationPort;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public PacketBase(PacketType type, string gameTitle, Version gameVerion, IPAddress sourceIP, 
            IPAddress destinationIP, ushort destinationPort)
        {
            _type = type;
            _gameTitle = gameTitle;
            _gameVersion = gameVerion;
            _sourceIP = sourceIP;
            _destinationIP = destinationIP;
            _destinationPort = destinationPort;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public PacketBase(PacketType type, PacketParser parser)
        {
            _type = type;
            _gameTitle = parser.GetString();
            _gameVersion = parser.GetVersion();
            _sourceIP = parser.GetIPAddress();
            _destinationIP = parser.GetIPAddress();
            _destinationPort = parser.GetUInt16();
        }

        public byte[] ToBytes()
        {
            PacketBuilder builder = new PacketBuilder();
            builder.AddInt32(PACKET_HEADER);
            builder.AddByte((byte)_type);
            builder.AddString(_gameTitle);
            builder.AddVersion(_gameVersion);
            builder.AddIPAddress(_sourceIP);
            builder.AddIPAddress(_destinationIP);
            builder.AddUInt16(_destinationPort);
            AddInstanceBytes(builder);
            builder.AddInt32(PACKET_FOOTER);
            return builder.ToBytes();
        }

        /// <summary>
        /// Deserializes packet from bytes.  Returns null if data invalid.
        /// </summary>
        public static PacketBase FromBytes(byte[] bytes)
        {
            try
            {
                PacketParser parser = new PacketParser(bytes);                
                int header = parser.GetInt32();
                if (header != PACKET_HEADER)
                    return null;                
                
                PacketType type = (PacketType)parser.GetByte();           
                switch (type)
                {
                    case PacketType.Discovery:
                        return new DiscoveryPacket(parser);

                    case PacketType.CommandRequest:
                        return new CommandRequestPacket(parser);

                    case PacketType.CommandResponse:
                        return new CommandResponsePacket(parser);

                    case PacketType.Data:
                        return new DataPacket(parser);

                    case PacketType.Heartbeat:
                        return new HeartbeatPacket(parser);

                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Adds instance bytes to packet builder.
        /// </summary>
        protected abstract void AddInstanceBytes(PacketBuilder builder);



    }

    /// <summary>
    /// Represents type of packet.
    /// </summary>
    public enum PacketType : byte
    {
        /// <summary>
        /// UDP broadcast packet for player discovery.
        /// </summary>
        Discovery = 1,

        /// <summary>
        /// Command request packet.
        /// </summary>
        CommandRequest = 2,

        /// <summary>
        /// Command response packet.
        /// </summary>
        CommandResponse = 4,

        /// <summary>
        /// General data packet, no response required.
        /// </summary>
        Data = 5,

        /// <summary>
        /// Sent at measured interval to maintain connection.
        /// </summary>
        Heartbeat = 6
    }
}

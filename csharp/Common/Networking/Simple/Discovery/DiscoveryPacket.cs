using System;
using System.Net;

namespace Common.Networking.Simple.Discovery
{
    /// <summary>
    /// Represents a UDP packet used for opponent discovery.
    /// </summary>
    public class DiscoveryPacket
    {
        //const
        private const int DISCOVERY_PACKET_HEADER = 8008135;
        
        //public
        public string GameTitle { get; }
        public Version GameVersion { get; }
        public IPAddress PlayerIP { get; }
        public int PlayerPort { get; }
        public string PlayerName { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public DiscoveryPacket(string gameTitle, Version gameVersion, IPAddress playerIP, int playerPort, string playerName)
        {
            GameTitle = gameTitle;
            GameVersion = gameVersion;
            PlayerIP = playerIP;
            PlayerPort = playerPort;
            PlayerName = playerName;
        }

        /// <summary>
        /// Serializes packet to bytes.
        /// </summary>
        public byte[] ToBytes()
        {
            PacketBuilder builder = new PacketBuilder();
            builder.AddInt32(DISCOVERY_PACKET_HEADER);
            builder.AddString(GameTitle);
            builder.AddVersion(GameVersion);
            builder.AddIPAddress(PlayerIP);
            builder.AddInt32(PlayerPort);
            builder.AddString(PlayerName);
            return builder.ToBytes();
        }

        /// <summary>
        /// Deserializes packet from bytes.  Returns null if data invalid.
        /// </summary>
        public static DiscoveryPacket FromBytes(byte[] bytes)
        {
            try
            {
                PacketParser parser = new PacketParser(bytes);
                int header = parser.GetInt32();
                if (header != DISCOVERY_PACKET_HEADER)
                    return null;
                DiscoveryPacket packet = new DiscoveryPacket(
                    gameTitle: parser.GetString(),
                    gameVersion: parser.GetVersion(),
                    playerIP: parser.GetIPAddress(),
                    playerPort: parser.GetInt32(),
                    playerName: parser.GetString());
                return packet;
            }
            catch
            {
                return null;
            }
        }

    }
}

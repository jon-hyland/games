using Common.Standard.Networking;
using Common.Standard.Networking.Packets;
using System;
using System.Net;

namespace Common.Standard.Game
{
    /// <summary>
    /// Represents a player, aka game instance (remote ip + game + game version).
    /// </summary>
    public class Player
    {
        //public
        public IPAddress IP { get; }
        public string GameTitle { get; }
        public Version GameVersion { get; }
        public string Name { get; set; }
        public DateTime LastDiscovery { get; set; }
        public TimeSpan TimeSinceLastDiscovery => DateTime.Now - LastDiscovery;
        public ushort InviteSequence { get; set; }
        public Client Client { get; set; }
        public int UniqueKey { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Player(IPAddress ip, string gameTitle, Version gameVersion, string name, ushort inviteSequence = 0, Client client = null)
        {
            IP = ip;
            GameTitle = gameTitle;
            GameVersion = gameVersion;
            Name = name;
            LastDiscovery = DateTime.Now;
            InviteSequence = inviteSequence;
            Client = client;
            UniqueKey = $"{IP}|{GameTitle}|{GameVersion}".GetHashCode();
        }

        /// <summary>
        /// Creates discovered player from packet.
        /// </summary>
        public static Player FromPacket(PacketBase packet, Client client)
        {
            return new Player(packet.SourceIP, packet.GameTitle, packet.GameVersion, packet.PlayerName, 0, client);
        }

        /// <summary>
        /// Serialize to bytes.
        /// </summary>
        public byte[] ToBytes()
        {
            PacketBuilder builder = new PacketBuilder();
            builder.AddIPAddress(IP);
            builder.AddString(GameTitle);
            builder.AddVersion(GameVersion);
            builder.AddString(Name);
            return builder.ToBytes();
        }

        /// <summary>
        /// Player from simple string.
        /// </summary>
        public static Player FromBytes(byte[] bytes)
        {
            PacketParser parser = new PacketParser(bytes);
            return new Player(
                ip: parser.GetIPAddress(),
                gameTitle: parser.GetString(),
                gameVersion: parser.GetVersion(),
                name: parser.GetString());
        }

    }
}

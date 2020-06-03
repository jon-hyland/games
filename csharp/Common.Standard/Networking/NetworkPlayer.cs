using Common.Standard.Networking.Packets;
using System;
using System.Net;

namespace Common.Standard.Networking
{
    /// <summary>
    /// Represents a player, aka game instance (remote ip + game + game version).
    /// </summary>
    public class NetworkPlayer
    {
        //public
        public IPAddress IP { get; }
        public string GameTitle { get; }
        public Version GameVersion { get; }
        public string Name { get; set; }
        public DateTime FirstHeartbeat { get; }
        public DateTime LastHeartbeat { get; set; }
        public TimeSpan TimeSinceLastHeartbeat => DateTime.Now - LastHeartbeat;
        public int InviteSequence { get; set; }
        public bool QuitGame { get; set; }
        public int UniqueKey { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public NetworkPlayer(IPAddress ip, string gameTitle, Version gameVersion, string name, int inviteSequence = 0)
        {
            IP = ip;
            GameTitle = gameTitle;
            GameVersion = gameVersion;
            Name = name;
            FirstHeartbeat = DateTime.Now;
            LastHeartbeat = DateTime.Now;
            InviteSequence = inviteSequence;
            QuitGame = false;
            UniqueKey = $"{IP}|{GameTitle}|{GameVersion}".GetHashCode();
        }

        /// <summary>
        /// Creates discovered player from packet.
        /// </summary>
        public static NetworkPlayer FromPacket(PacketBase packet)
        {
            return new NetworkPlayer(packet.SourceIP, packet.GameTitle, packet.GameVersion, packet.PlayerName, 0);
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
        public static NetworkPlayer FromBytes(byte[] bytes)
        {
            PacketParser parser = new PacketParser(bytes);
            return new NetworkPlayer(
                ip: parser.GetIPAddress(),
                gameTitle: parser.GetString(),
                gameVersion: parser.GetVersion(),
                name: parser.GetString());
        }

    }
}

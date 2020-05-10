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
        public IClient Client { get; set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Player(IPAddress ip, string gameTitle, Version gameVersion, string name, ushort inviteSequence = 0, IClient client = null)
        {
            GameTitle = gameTitle;
            GameVersion = gameVersion;
            IP = ip;
            Name = name;
            LastDiscovery = DateTime.Now;
            InviteSequence = inviteSequence;
            Client = client;
        }

        /// <summary>
        /// Creates discovered player from packet.
        /// </summary>
        public static Player FromPacket(PacketBase packet, IClient client)
        {
            return new Player(packet.SourceIP, packet.GameTitle, packet.GameVersion, packet.PlayerName, 0, client);
        }

        /// <summary>
        /// Unique key representing player (not including name, which can change).
        /// </summary>
        public int UniqueKey => $"{IP}|{GameTitle}|{GameVersion}".GetHashCode();
    }
}

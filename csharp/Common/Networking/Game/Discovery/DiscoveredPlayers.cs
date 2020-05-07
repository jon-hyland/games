using Common.Networking.Game.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Common.Networking.Game.Discovery
{
    /// <summary>
    /// Stores and exposes list of discovered players.
    /// </summary>
    public class DiscoveredPlayers
    {
        //private
        private readonly List<Player> _players = new List<Player>();

        /// <summary>
        /// Returns shallow copy of discovered player list, with matching game title and version,
        /// where player IP is not local IP (this happens sometimes when UDP broadcast bounces
        /// back).  Returns most recent 5 (or custom) players.
        /// </summary>
        public List<Player> GetPlayers(string gameTitle, Version gameVersion, IPAddress localIP, int top = 5)
        {
            lock (_players)
            {
                return _players
                    .Where(p => p.GameTitle.Equals(gameTitle))
                    .Where(p => p.GameVersion.Equals(gameVersion))
                    .Where(p => !p.IP.Equals(localIP))
                    .OrderByDescending(p => p.LastDiscovery)
                    .Take(top)
                    .ToList();
            }
        }

        /// <summary>
        /// Returns count of discovered player list, with matching game title and version,
        /// where player IP is not local IP (this happens sometimes when UDP broadcast bounces
        /// back).
        /// </summary>
        public int GetPlayerCount(string gameTitle, Version gameVersion, IPAddress localIP, int top = 5)
        {
            lock (_players)
            {
                return _players
                    .Where(p => p.GameTitle.Equals(gameTitle))
                    .Where(p => p.GameVersion.Equals(gameVersion))
                    .Where(p => !p.IP.Equals(localIP))
                    .Take(top)
                    .Count();
            }
        }

        /// <summary>
        /// Adds or updates a discovered player.
        /// </summary>
        public void AddOrUpdatePlayer(Player player)
        {
            lock (_players)
            {
                Player existing = _players
                    .Where(p => p.UniqueKey == player.UniqueKey)
                    .FirstOrDefault();

                if (existing != null)
                {
                    if (existing.Name != player.Name)
                        existing.Name = player.Name;
                    TimeSpan sinceLastDiscovery = DateTime.Now - existing.LastDiscovery;
                    if (sinceLastDiscovery.TotalHours > 24)
                        existing.LastDiscovery = DateTime.Now;
                }
                else
                {
                    _players.Add(player);
                }
            }
        }
    }

    /// <summary>
    /// Represents a discovered remote player.
    /// </summary>
    public class Player
    {
        //public
        public string GameTitle { get; }
        public Version GameVersion { get; }
        public IPAddress IP { get; }
        public int Port { get; }
        public string Name { get; set; }
        public DateTime LastDiscovery { get; set; }
        public ushort InviteSequence { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Player(string gameTitle, Version gameVersion, IPAddress ip, int port, string name, ushort inviteSequence = 0)
        {
            GameTitle = gameTitle;
            GameVersion = gameVersion;
            IP = ip;
            Port = port;
            Name = name;
            LastDiscovery = DateTime.Now;
            InviteSequence = inviteSequence;
        }

        /// <summary>
        /// Creates discovered player from packet.
        /// </summary>
        public static Player FromPacket(DiscoveryPacket packet)
        {
            return new Player(packet.GameTitle, packet.GameVersion, packet.PlayerIP, packet.PlayerPort, packet.PlayerName);
        }

        /// <summary>
        /// Unique key representing player (not including name, which can change).
        /// </summary>
        public int UniqueKey => $"{GameTitle}|{GameVersion}|{IP}|{Port}".GetHashCode();
    }
}

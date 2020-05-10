using Common.Standard.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.Game
{
    /// <summary>
    /// Stores and exposes list of discovered players.
    /// </summary>
    public class ConnectedPlayers
    {
        //private
        private readonly List<Player> _players = new List<Player>();

        /// <summary>
        /// Returns list of matching players.
        /// </summary>
        public List<Player> GetPlayers(string gameTitle, Version gameVersion, int top = 5)
        {
            lock (_players)
            {
                RemoveExpiredPlayers();
                return _players
                    .Where(p => p.GameTitle.Equals(gameTitle))
                    .Where(p => p.GameVersion.Equals(gameVersion))
                    .OrderByDescending(p => p.LastDiscovery)
                    .Take(top)
                    .ToList();
            }
        }

        /// <summary>
        /// Returns count of matching players.
        /// </summary>
        public int GetPlayerCount(string gameTitle, Version gameVersion, int top = 5)
        {
            lock (_players)
            {
                RemoveExpiredPlayers();
                return _players
                    .Where(p => p.GameTitle.Equals(gameTitle))
                    .Where(p => p.GameVersion.Equals(gameVersion))
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
                    if (sinceLastDiscovery.TotalSeconds > 60)
                        existing.LastDiscovery = DateTime.Now;
                }
                else
                {
                    _players.Add(player);
                }

                RemoveExpiredPlayers();
            }
        }

        /// <summary>
        /// Removes any players that haven't checked in within the past 5 minutes.
        /// </summary>
        private void RemoveExpiredPlayers()
        {
            lock (_players)
            {
                List<Player> expired = _players
                    .Where(p => p.TimeSinceLastDiscovery.TotalMinutes > 5)
                    .ToList();
                foreach (Player p in expired)
                    _players.Remove(p);
            }
        }


    }
}

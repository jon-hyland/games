﻿using Common.Standard.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bricker.Rendering.Properties
{
    /// <summary>
    /// Stores message properties for rendering.
    /// </summary>
    public class LobbyProperties
    {
        //private
        private readonly List<NetworkPlayer> _players = new List<NetworkPlayer>();
        private int _playerIndex = -1;
        private int _buttonIndex = 0;
        private DateTime _lastPlayerUpdate = DateTime.MinValue;

        //public
        public int PlayerIndex => _playerIndex;
        public int ButtonIndex => _buttonIndex;
        public DateTime LastPlayerUpdate => _lastPlayerUpdate;
        public TimeSpan TimeSinceLastPlayerUpdate => DateTime.Now - _lastPlayerUpdate;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public LobbyProperties()
        {
        }

        /// <summary>
        /// Updates player list.
        /// </summary>
        public void UpdatePlayers(IEnumerable<NetworkPlayer> players)
        {
            lock (this)
            {
                _lastPlayerUpdate = DateTime.Now;
                int prevCount = _players.Count;
                _players.Clear();
                _players.AddRange(players);
                if ((_players.Count > 0) && (prevCount == 0))
                {
                    if (_playerIndex == -1)
                        _playerIndex = 0;
                    if (_buttonIndex == 0)
                        _buttonIndex = 1;
                }
            }
        }

        /// <summary>
        /// Gets copy of player list.
        /// </summary>
        public IReadOnlyList<NetworkPlayer> GetPlayers()
        {
            lock (this)
            {
                return _players.ToList();
            }
        }

        /// <summary>
        /// Increments player index.
        /// </summary>
        public void IncrementPlayerIndex()
        {
            lock (this)
            {
                _playerIndex++;
                if (_playerIndex >= _players.Count)
                    _playerIndex = 0;
            }
        }

        /// <summary>
        /// Increments player index.
        /// </summary>
        public void DecrementPlayerIndex()
        {
            lock (this)
            {
                _playerIndex--;
                if (_playerIndex < 0)
                    _playerIndex = _players.Count - 1;
            }
        }

        /// <summary>
        /// Increments button index.
        /// </summary>
        public void IncrementButtonIndex()
        {
            lock (this)
            {
                int maxIndex = _players.Count > 0 ? 1 : 0;
                _buttonIndex++;
                if (_buttonIndex > maxIndex)
                    _buttonIndex = 0;
            }
        }

        /// <summary>
        /// Increments button index.
        /// </summary>
        public void DecrementButtonIndex()
        {
            lock (this)
            {
                int maxIndex = _players.Count > 0 ? 1 : 0;
                _buttonIndex--;
                if (_buttonIndex < 0)
                    _buttonIndex = maxIndex;
            }
        }

    }
}

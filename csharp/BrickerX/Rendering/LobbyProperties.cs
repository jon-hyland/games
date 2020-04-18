using Bricker.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bricker.Rendering
{
    /// <summary>
    /// Stores message properties for rendering.
    /// </summary>
    public class LobbyProperties
    {
        //private
        private int _opponentIndex = 0;
        private int _buttonIndex = 0;

        //public
        public int OpponentIndex => _opponentIndex;
        public int ButtonIndex => _buttonIndex;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public LobbyProperties()
        {
        }

        /// <summary>
        /// Increments opponent index.
        /// </summary>
        public void IncrementOpponentIndex()
        {
            lock (this)
            {
                _opponentIndex++;
                if (_opponentIndex >= NetworkDiscovery.RemoteInstanceCount)
                    _opponentIndex = 0;
            }
        }

        /// <summary>
        /// Increments opponent index.
        /// </summary>
        public void DecrementOpponentIndex()
        {
            lock (this)
            {
                _opponentIndex--;
                if (_opponentIndex < 0)
                    _opponentIndex = NetworkDiscovery.RemoteInstanceCount - 1;
            }
        }

        /// <summary>
        /// Increments button index.
        /// </summary>
        public void IncrementButtonIndex()
        {
            lock (this)
            {
                _buttonIndex++;
                if (_buttonIndex > 1)
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
                _buttonIndex--;
                if (_buttonIndex < 0)
                    _buttonIndex = 1;
            }
        }

    }
}

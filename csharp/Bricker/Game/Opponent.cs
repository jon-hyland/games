using Bricker.Networking;
using System;

namespace Bricker.Game
{
    /// <summary>
    /// Represents your opponent in a two-player game.  An opponents stats
    /// are populated via TCP packets received from other computer.
    /// </summary>
    public class Opponent
    {
        //private
        private readonly string _initials;
        private readonly string _ipAddress;
        private DateTime _lastPacketTime;
        private readonly byte[,] _matrix;
        private int _level;
        private int _lines;
        private int _score;

        //public
        public string Initials => _initials;
        public string IPAddress => _ipAddress;
        public TimeSpan TimeSinceLastPacket => DateTime.Now - _lastPacketTime;
        public int Level => _level;
        public int Lines => _lines;
        public int Score => _score;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Opponent(string initials, string ipAddress)
        {
            _initials = initials;
            _ipAddress = ipAddress;
            _lastPacketTime = DateTime.MinValue;
            _matrix = new byte[10, 20];
        }

        /// <summary>
        /// Returns thread-safe shallow copy of opponents matrix.
        /// </summary>
        public byte[,] GetMatrix()
        {
            byte[,] matrix;
            lock (this)
            {
                matrix = (byte[,])_matrix.Clone();
            }
            return matrix;
        }

        /// <summary>
        /// Processes an incoming packet.
        /// </summary>
        public void ProcessPacket(Packet packet)
        {
            lock (this)
            {
                _lastPacketTime = DateTime.Now;
                for (int x = 0; x < 10; x++)
                    for (int y = 0; y < 20; y++)
                        _matrix[x, y] = 0;
                _level = 1;
                _lines = 0;
                _score = 0;
            }
        }






    }
}

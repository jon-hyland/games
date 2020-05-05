using Common.Networking.Game.Discovery;
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
        private readonly Player _player;
        private DateTime _lastPacketTime;
        private readonly byte[,] _matrix;
        private int _level;
        private int _lines;
        private int _score;
        private int _linesSent;

        //public
        public Player Player => _player;
        public DateTime LastPacketTime => _lastPacketTime;
        public TimeSpan TimeSinceLastPacket => DateTime.Now - _lastPacketTime;
        public int Level => _level;
        public int Lines => _lines;
        public int Score => _score;
        public int LinesSent => _linesSent;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Opponent(Player player)
        {
            _player = player;
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

        //public

        ///// <summary>
        ///// Processes an incoming packet.
        ///// </summary>
        //public void ProcessPacket(Packet packet)
        //{
        //    lock (this)
        //    {
        //        _lastPacketTime = DateTime.Now;
        //        for (int x = 0; x < 10; x++)
        //            for (int y = 0; y < 20; y++)
        //                _matrix[x, y] = 0;
        //        _level = 1;
        //        _lines = 0;
        //        _score = 0;
        //    }
        //}






    }
}

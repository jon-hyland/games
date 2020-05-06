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
        private bool _gameOver;

        //public
        public Player Player => _player;
        public DateTime LastPacketTime => _lastPacketTime;
        public TimeSpan TimeSinceLastPacket => DateTime.Now - _lastPacketTime;
        public int Level => _level;
        public int Lines => _lines;
        public int Score => _score;
        public int LinesSent => _linesSent;
        public bool GameOver => _gameOver;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Opponent(Player player)
        {
            _player = player;
            _lastPacketTime = DateTime.MinValue;
            _matrix = new byte[12, 22];
            _level = 1;
            _lines = 0;
            _score = 0;
            _linesSent = 0;
            _gameOver = false;
        }

        /// <summary>
        /// Resets opponents values to start new game.
        /// </summary>
        public void Reset()
        {
            for (int x = 0; x < 12; x++)
                for (int y = 0; y < 22; y++)
                    _matrix[x, y] = 0;
            _level = 1;
            _lines = 0;
            _score = 0;
            _linesSent = 0;
            _gameOver = false;
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
        /// Updates opponent after status packet received.
        /// </summary>
        public void UpdateOpponent(byte[,] matrix, int level, int lines, int score, int linesSent, bool gameOver)
        {
            lock (this)
            {
                for (int x = 0; x < matrix.GetLength(0); x++)
                    for (int y = 0; y < matrix.GetLength(1); y++)
                        _matrix[x, y] = matrix[x, y];
                _level = level;
                _lines = lines;
                _score = score;
                _linesSent = linesSent;
                _gameOver = gameOver;
            }
        }

    }
}

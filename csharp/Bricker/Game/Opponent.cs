using Common.Standard.Game;

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
        private readonly Space[,] _matrix;
        private int _level;
        private int _lines;
        private int _score;
        private int _linesSent;
        private int _lastLinesSent;
        private bool _gameOver;

        //public
        public Player Player => _player;
        public int Level => _level;
        public int Lines => _lines;
        public int Score => _score;
        public int LinesSent => _linesSent;
        public int LastLinesSent => _lastLinesSent;
        public bool GameOver => _gameOver;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Opponent(Player player)
        {
            _player = player;
            _matrix = new Space[12, 22];
            _level = 1;
            _lines = 0;
            _score = 0;
            _linesSent = 0;
            _lastLinesSent = 0;
            _gameOver = false;
        }

        /// <summary>
        /// Resets opponents values to start new game.
        /// </summary>
        public void Reset()
        {
            lock (this)
            {
                for (int x = 0; x < 12; x++)
                    for (int y = 0; y < 22; y++)
                        _matrix[x, y] = Space.Empty;
                _level = 1;
                _lines = 0;
                _score = 0;
                _linesSent = 0;
                _lastLinesSent = 0;
                _gameOver = false;
            }
        }

        /// <summary>
        /// Returns thread-safe shallow copy of opponents matrix.
        /// </summary>
        public Space[,] GetMatrix()
        {
            Space[,] matrix;
            lock (this)
            {
                matrix = (Space[,])_matrix.Clone();
            }
            return matrix;
        }

        /// <summary>
        /// Sets game over flag (opponent has finished).
        /// </summary>
        public void SetGameOver()
        {
            lock (this)
            {
                _gameOver = true;
            }
        }

        /// <summary>
        /// Sets last lines sent.
        /// </summary>
        public void SetLastLinesSent(int value)
        {
            lock (this)
            {
                _lastLinesSent = value;
            }
        }

        /// <summary>
        /// Updates opponent after status packet received.
        /// </summary>
        public void UpdateOpponent(Space[,] matrix, int level, int lines, int score, int linesSent)
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
            }
        }

    }
}

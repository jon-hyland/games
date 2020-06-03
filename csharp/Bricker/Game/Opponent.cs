using Common.Standard.Networking;

namespace Bricker.Game
{
    /// <summary>
    /// Represents your opponent in a two-player game.  An opponents stats
    /// are populated via TCP packets received from other computer.
    /// </summary>
    public class Opponent
    {
        //private
        private readonly NetworkPlayer _networkPlayer;
        private readonly Space[,] _grid;
        private int _level;
        private int _lines;
        private int _score;
        private int _linesSent;
        private int _lastLinesSent;
        private bool _gameOver;

        //public
        public NetworkPlayer NetworkPlayer => _networkPlayer;
        public Space[,] Grid => _grid;
        public int Level => _level;
        public int Lines => _lines;
        public int Score => _score;
        public int LinesSent => _linesSent;
        public int LastLinesSent => _lastLinesSent;
        public bool GameOver => _gameOver;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Opponent(NetworkPlayer networkPlayer)
        {
            _networkPlayer = networkPlayer;
            _grid = new Space[12, 22];
            _level = 1;
            _lines = 0;
            _score = 0;
            _linesSent = 0;
            _lastLinesSent = 0;
            _gameOver = false;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Opponent(NetworkPlayer networkPlayer, Space[,] grid, int level, int lines, int score, int linesSent, int lastLinesSent, bool gameOver)
        {
            _networkPlayer = networkPlayer;
            _grid = grid;
            _level = level;
            _lines = lines;
            _score = score;
            _linesSent = linesSent;
            _lastLinesSent = lastLinesSent;
            _gameOver = gameOver;
        }

        /// <summary>
        /// Resets opponents values to start new game.
        /// </summary>
        public void Reset()
        {
            for (int x = 0; x < 12; x++)
                for (int y = 0; y < 22; y++)
                    _grid[x, y] = Space.Empty;
            _level = 1;
            _lines = 0;
            _score = 0;
            _linesSent = 0;
            _lastLinesSent = 0;
            _gameOver = false;
        }

        /// <summary>
        /// Sets game over flag (opponent has finished).
        /// </summary>
        public void SetGameOver()
        {
            _gameOver = true;
        }

        /// <summary>
        /// Sets last lines sent.
        /// </summary>
        public void SetLastLinesSent(int value)
        {
            _lastLinesSent = value;
        }

        /// <summary>
        /// Updates opponent after status packet received.
        /// </summary>
        public void UpdateOpponent(Space[,] matrix, int level, int lines, int score, int linesSent)
        {
            for (int x = 0; x < matrix.GetLength(0); x++)
                for (int y = 0; y < matrix.GetLength(1); y++)
                    _grid[x, y] = matrix[x, y];
            _level = level;
            _lines = lines;
            _score = score;
            _linesSent = linesSent;
        }

        /// <summary>
        /// Returns thread-safe copy of opponent.
        /// </summary>
        public Opponent Clone()
        {
            return new Opponent(_networkPlayer, _grid, _level, _lines, _score, _linesSent, _lastLinesSent, _gameOver);
        }

    }
}

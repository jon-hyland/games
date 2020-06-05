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
        private NetworkPlayer _networkPlayer;
        private readonly Space[,] _grid;
        private readonly PlayerStats _stats;
        private bool _gameOver;

        //public
        public NetworkPlayer NetworkPlayer => _networkPlayer;
        //public Space[,] Grid => _grid;
        public PlayerStats Stats => _stats;
        public bool GameOver => _gameOver;
        public bool Exists => _networkPlayer != null;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Opponent(NetworkPlayer networkPlayer)
        {
            _networkPlayer = networkPlayer;
            _grid = new Space[12, 22];
            _stats = new PlayerStats();
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
                        _grid[x, y] = Space.Empty;
                _stats.Reset();
                _gameOver = false;
            }
        }

        /// <summary>
        /// Sets (or clears) opponent's network player instance.
        /// </summary>
        public void SetNetworkPlayer(NetworkPlayer networkPlayer)
        {
            lock (this)
            {
                _networkPlayer = networkPlayer;
            }
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
                _stats.SetLastLinesSent(value);
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
                        _grid[x, y] = matrix[x, y];

                _stats.SetLevel(level);
                _stats.SetLines(lines);
                _stats.SetScore(score);
                _stats.SetLinesSent(linesSent);
            }
        }

        /// <summary>
        /// Gets safe copies of objects used for frame rendering.
        /// </summary>
        public void GetRenderObjects(out Space[,] grid, out PlayerStats stats, out string name)
        {
            lock (this)
            {
                if (_networkPlayer != null)
                {
                    grid = (Space[,])_grid.Clone();
                    stats = _stats;
                    name = _networkPlayer.Name;
                }
                else
                {
                    grid = null;
                    stats = null;
                    name = null;
                }
            }
        }


    }
}

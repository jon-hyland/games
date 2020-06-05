using Common.Standard.Configuration;
using Common.Standard.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Bricker.Game
{
    /// <summary>
    /// Represents the player, their 10x20 game matrix, game variables, etc.
    /// </summary>
    public class Player
    {
        //private
        private NetworkPlayer _networkPlayer;
        private readonly double[] _levelDropIntervals;
        private readonly Random _random;
        private readonly Space[,] _grid;
        private readonly Queue<Brick> _nextBricks;
        private readonly PlayerStats _stats;
        private Brick _brick;
        private Brick _hold;

        //public
        public PlayerStats Stats => _stats;
        public Space this[int x, int y] { get { lock (this) { return _grid[x, y]; } } set { lock (this) { _grid[x, y] = value; } } }

        #region Constructor / Reset

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Player()
        {
            _levelDropIntervals = new double[10];
            _random = new Random();
            _grid = new Space[12, 22];
            _nextBricks = new Queue<Brick>();
            _stats = new PlayerStats();
            _brick = null;
            _hold = null;
            Reset(false);

            double interval = 2000;
            for (int i = 0; i < 10; i++)
            {
                interval *= 0.8;
                _levelDropIntervals[i] = interval;
            }
        }

        /// <summary>
        /// Resets the game.
        /// </summary>
        public void Reset(bool spawnBrick = true)
        {
            lock (this)
            {
                ClearGrid(_grid);
                for (int x = 0; x < 12; x++)
                {
                    _grid[x, 0] = Space.Edge;
                    _grid[x, 22 - 1] = Space.Edge;
                }
                for (int y = 0; y < 22; y++)
                {
                    _grid[0, y] = Space.Edge;
                    _grid[12 - 1, y] = Space.Edge;
                }
                _brick = null;
                _hold = null;
                _stats.Reset();
                if (spawnBrick)
                    SpawnBrick();
            }
        }

        #endregion

        #region Grid Logic

        /// <summary>
        /// Fills grid with empty spaces.
        /// </summary>
        private static void ClearGrid(Space[,] grid)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    grid[x, y] = Space.Empty;
        }

        /// <summary>
        /// Returns copy of grid.
        /// </summary>
        private Space[,] GetGrid(bool includeBrick, bool includeGhost)
        {
            lock (this)
            {
                Space[,] grid = (Space[,])_grid.Clone();
                if ((includeBrick) && (_brick != null))
                {
                    for (int x = 0; x < _brick.Width; x++)
                    {
                        for (int y = 0; y < _brick.Height; y++)
                        {
                            if (_brick.Grid[x, y].IsSolid())
                            {
                                int gx = x + _brick.X;
                                int gy = y + _brick.Y;
                                int gyg = y + _brick.YGhost;
                                if ((includeGhost) && (gx >= 0) && (gx <= 11) && (gyg >= 0) && (gyg <= 21))
                                    grid[gx, gyg] = (Space)((byte)_brick.Grid[x, y] + 7);
                                if ((gx >= 0) && (gx <= 11) && (gy >= 0) && (gy <= 21))
                                    grid[gx, gy] = _brick.Grid[x, y];
                            }
                        }
                    }
                }
                return grid;
            }
        }

        /// <summary>
        /// Returns list of all solid rows (ready to be cleared).
        /// </summary>
        public List<int> IdentifySolidRows()
        {
            lock (this)
            {
                List<int> rowsToErase = new List<int>();
                for (int y = 1; y < 21; y++)
                {
                    bool solid = true;
                    for (int x = 1; x < 11; x++)
                        if (!_grid[x, y].IsSolid())
                            solid = false;
                    if (solid)
                        rowsToErase.Add(y);
                }
                return rowsToErase;
            }
        }

        /// <summary>
        /// Animates erasure of filled rows.
        /// </summary>
        public void EraseFilledRows(IEnumerable<int> rowsToErase)
        {
            DateTime start = DateTime.Now;
            double xPerSecond = 50;
            int x = 0;
            while (x < 10)
            {
                Thread.Sleep(5);
                lock (this)
                {
                    TimeSpan elapsed = DateTime.Now - start;
                    int expectedX = (int)Math.Round(xPerSecond * elapsed.TotalSeconds);
                    while (x < expectedX)
                    {
                        x++;
                        if ((x < 1) || (x > 10))
                            break;
                        foreach (int y in rowsToErase)
                            _grid[x, y] = Space.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Drops hanging pieces, bottom-most row.
        /// </summary>
        public bool DropGridOnce()
        {
            lock (this)
            {
                int topFilledRow = 0;
                for (int row = 1; row <= 20; row++)
                {
                    bool empty = true;
                    for (int x = 1; x <= 10; x++)
                    {
                        if (_grid[x, row].IsSolid())
                        {
                            empty = false;
                            break;
                        }
                    }
                    if (!empty)
                    {
                        topFilledRow = row;
                        break;
                    }
                }
                if (topFilledRow == 0)
                    return false;
                int bottomEmptyRow = 0;
                for (int row = 20; row > (topFilledRow - 1); row--)
                {
                    bool empty = true;
                    for (int x = 1; x <= 10; x++)
                    {
                        if (_grid[x, row].IsSolid())
                        {
                            empty = false;
                            break;
                        }
                    }
                    if (empty)
                    {
                        bottomEmptyRow = row;
                        break;
                    }
                }
                if (bottomEmptyRow == 0)
                    return false;
                for (int y = bottomEmptyRow; y > 1; y--)
                    for (int x = 1; x <= 10; x++)
                        _grid[x, y] = _grid[x, y - 1];
                for (int x = 1; x <= 10; x++)
                    _grid[x, 1] = Space.Empty;
                return true;
            }
        }

        /// <summary>
        /// Adds lines sent by opponent.
        /// </summary>
        public bool AddSentLines(int newLines)
        {
            //vars
            int gapIndex = _random.Next(10) + 1;
            bool outBounds = false;

            //move lines up
            lock (this)
            {
                for (int y = 1; y <= 20; y++)
                {
                    for (int x = 1; x <= 10; x++)
                    {
                        if (_grid[x, y].IsSolid())
                        {
                            int newY = y - newLines;
                            if (newY > 0)
                                _grid[x, newY] = _grid[x, y];
                            else
                                outBounds = true;
                            _grid[x, y] = Space.Empty;
                        }
                    }
                }
            }

            //add lines (animated)
            DateTime start = DateTime.Now;
            double xPerSecond = 50;
            int xx = 0;
            while (xx < 10)
            {
                Thread.Sleep(5);
                lock (this)
                {
                    TimeSpan elapsed = DateTime.Now - start;
                    int expectedX = (int)Math.Round(xPerSecond * elapsed.TotalSeconds);
                    while (xx < expectedX)
                    {
                        xx++;
                        if ((xx < 1) || (xx > 10))
                            break;
                        for (int y = 20; y > 20 - newLines; y--)
                            _grid[xx, y] = (xx != gapIndex ? Space.Sent : Space.Empty);
                    }
                }
            }

            //return
            return outBounds;
        }

        #endregion

        #region Brick Logic

        /// <summary>
        /// Returns a copy of the live brick, or null if one doesn't exist.
        /// </summary>
        public Brick GetBrick()
        {
            lock (this)
            {
                return _brick?.Clone();
            }
        }

        /// <summary>
        /// Returns a copy of the held brick, or null if one doesn't exist.
        /// </summary>
        private Brick GetHold()
        {
            lock (this)
            {
                return _hold?.Clone();
            }
        }

        /// <summary>
        /// Grabs the next brick in line, makes it the current brick.
        /// Refills the queue with six items.
        /// Returns true if current brick collides with matrix shape.
        /// </summary>
        public bool SpawnBrick()
        {
            lock (this)
            {
                while (_nextBricks.Count < 7)
                    _nextBricks.Enqueue(new Brick((Space)(_random.Next(7) + 1)));
                _brick = _nextBricks.Dequeue();
                _brick.Spawned(_grid);
                return Brick.Collision(_grid, _brick.Grid, _brick.X, _brick.Y);
            }
        }

        /// <summary>
        /// Moves resting brick to matrix.
        /// </summary>
        public void AddBrickToMatrix()
        {
            lock (this)
            {
                if (_brick == null)
                    return;

                for (int x = 0; x < _brick.Width; x++)
                    for (int y = 0; y < _brick.Height; y++)
                        if (_brick.Grid[x, y].IsSolid())
                            _grid[x + _brick.X, y + _brick.Y] = _brick.Grid[x, y];

                _brick = null;
            }
        }

        /// <summary>
        /// Returns up to six next bricks.
        /// </summary>
        public Brick[] GetNextBricks()
        {
            lock (this)
            {
                return _nextBricks.Take(6).ToArray();
            }
        }

        public bool IsBrickDropTime(bool resting, bool moveAfterResting)
        {
            lock (this)
            {
                if (_brick != null)
                {
                    double dropIntervalMs = _levelDropIntervals[_stats.Level - 1];
                    if (resting && moveAfterResting)
                        dropIntervalMs *= 2;
                    double elapsedMs = (DateTime.Now - _brick.LastDropTime).TotalMilliseconds;
                    bool dropTime = elapsedMs >= dropIntervalMs;
                    return dropTime;
                }
                return false;
            }
        }

        /// <summary>
        /// Moves brick to the left.
        /// </summary>
        public void MoveBrickLeft(out bool moved, out bool resting)
        {
            lock (this)
            {
                moved = false;
                resting = false;
                if (_brick != null)
                    _brick.MoveLeft(_grid, out moved, out resting);
            }
        }

        /// <summary>
        /// Moves brick to the righ.
        /// </summary>
        public void MoveBrickRight(out bool moved, out bool resting)
        {
            lock (this)
            {
                moved = false;
                resting = false;
                if (_brick != null)
                    _brick.MoveRight(_grid, out moved, out resting);
            }
        }

        /// <summary>
        /// Moves brick down.  Returns true if brick hits bottom.
        /// </summary>
        public void MoveBrickDown(out bool hit, out bool resting)
        {
            lock (this)
            {
                hit = false;
                resting = false;
                if (_brick != null)
                    _brick.MoveDown(_grid, out hit, out resting);
            }
        }

        /// <summary>
        /// Rotates the live brick, if one exists.
        /// </summary>
        public void RotateBrick(out bool resting)
        {
            lock (this)
            {
                resting = false;
                if (_brick != null)
                    _brick.Rotate(_grid, out resting);
            }


        }

        /// <summary>
        /// Takes the currently live brick and puts it in hold.  If there's already
        /// a brick held, the two swap.  Returns true if swap causes collision.
        /// </summary>
        public void HoldBrick(out bool collision, out bool resting)
        {
            collision = false;
            resting = false;
            if (_brick == null)
                return;

            int oX = _brick.X;
            int oY = _brick.Y;

            if (_hold == null)
            {
                _hold = _brick;
                SpawnBrick();
                _brick.SetXY(oX, oY, _grid);
                collision = Brick.Collision(_grid, _brick.Grid, _brick.X, _brick.Y);
                resting = Brick.Resting(_grid, _brick.Grid, _brick.X, _brick.Y);
                return;
            }
            else
            {
                Brick temp = _hold;
                _hold = _brick;
                _brick = temp;
                _brick.SetXY(oX, oY, _grid);
                collision = Brick.Collision(_grid, _brick.Grid, _brick.X, _brick.Y);
                resting = Brick.Resting(_grid, _brick.Grid, _brick.X, _brick.Y);
                return;
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Increments current score.
        /// </summary>
        public void IncrementScore(int value)
        {
            lock (this)
            {
                _stats.IncrementScore(value);
            }
        }

        /// <summary>
        /// Increments cleared line count, and sets level.
        /// Returns true on level increase.
        /// </summary>
        public bool IncrementLinesAndLevel(int value)
        {
            lock (this)
            {
                return _stats.IncrementLinesAndLevel(value);
            }
        }

        /// <summary>
        /// Sets the current level.
        /// </summary>
        public bool SetLevel(int level)
        {
            lock (this)
            {
                return _stats.SetLevel(level);
            }
        }

        /// <summary>
        /// Increments lines sent (two-player mode).
        /// </summary>
        public void IncrementLinesSent(int value)
        {
            lock (this)
            {
                _stats.IncrementLinesSent(value);
            }
        }

        /// <summary>
        /// Sets lines sent (two-player mode).
        /// </summary>
        public void SetLinesSent(int value)
        {
            lock (this)
            {
                _stats.SetLinesSent(value);
            }
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Gets safe copies of objects used for frame rendering.
        /// </summary>
        public void GetRenderObjects(out Space[,] matrixGrid, out Brick holdBrick, out Brick[] nextBricks, out PlayerStats stats)
        {
            lock (this)
            {
                matrixGrid = GetGrid(includeBrick: true, includeGhost: GameConfig.Instance.ShowGhost);
                holdBrick = GetHold();
                nextBricks = GetNextBricks();
                stats = _stats;
            }
        }

        #endregion



    }
}

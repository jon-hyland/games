using System;
using System.Collections.Generic;
using System.Linq;

namespace Bricker.Game
{
    /// <summary>
    /// Represents the 10x20 game matrix, game variables, and some game logic.
    /// </summary>
    public class Matrix
    {
        //private
        private readonly Random _random;
        private readonly byte[,] _grid;
        private readonly Queue<Brick> _nextBricks;
        private Brick _brick;
        private Brick _hold;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Matrix()
        {
            _random = new Random();
            _grid = new byte[12, 22];
            _nextBricks = new Queue<Brick>();
            _brick = null;
            _hold = null;
            NewGame(false);
        }

        /// <summary>
        /// XY class indexer that accesses the underlying grid, locking for thread safety.
        /// </summary>
        public byte this[int x, int y]
        {
            get
            {
                lock (this)
                {
                    return _grid[x, y];
                }
            }
            set
            {
                lock (this)
                {
                    _grid[x, y] = value;
                }
            }
        }

        /// <summary>
        /// Returns copy of grid.
        /// </summary>
        public byte[,] GetGrid(bool includeBrick)
        {
            lock (this)
            {
                byte[,] grid = (byte[,])_grid.Clone();
                if ((includeBrick) && (_brick != null))
                {
                    for (int x = 0; x < _brick.Width; x++)
                        for (int y = 0; y < _brick.Height; y++)
                            if (_brick.Grid[x, y] > 0)
                            {
                                int gx = x + _brick.X;
                                int gy = y + _brick.Y;
                                if ((gx < 0) || (gx > 11) || (gy < 0) || (gy > 21))
                                    continue;
                                grid[gx, gy] = _brick.Grid[x, y];
                            }
                }
                return grid;
            }
        }

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
        public Brick GetHold()
        {
            lock (this)
            {
                return _hold?.Clone();
            }
        }

        /// <summary>
        /// Resets the game.
        /// </summary>
        public void NewGame(bool spawnBrick = true)
        {
            lock (this)
            {
                Buffer.BlockCopy(Enumerable.Repeat((byte)0, 12 * 22).ToArray(), 0, _grid, 0, 12 * 22);
                for (int x = 0; x < 12; x++)
                {
                    _grid[x, 0] = 8;
                    _grid[x, 22 - 1] = 8;
                }
                for (int y = 0; y < 22; y++)
                {
                    _grid[0, y] = 8;
                    _grid[12 - 1, y] = 8;
                }
                _brick = null;
                _hold = null;
                if (spawnBrick)
                    SpawnBrick(1);
            }
        }

        /// <summary>
        /// Grabs the next brick in line, makes it the current brick.
        /// Refills the queue with six items.
        /// Returns true if current brick collides with matrix shape.
        /// </summary>
        public bool SpawnBrick(int level)
        {
            lock (this)
            {
                while (_nextBricks.Count < 7)
                    _nextBricks.Enqueue(new Brick(_random.Next(7) + 1));
                _brick = _nextBricks.Dequeue();
                _brick.Spawned(level);
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
                        if (_brick.Grid[x, y] > 0)
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

        /// <summary>
        /// Moves brick to the left.
        /// </summary>
        public void MoveBrickLeft()
        {
            lock (this)
            {
                if (_brick != null)
                    _brick.MoveLeft(_grid);
            }
        }

        /// <summary>
        /// Moves brick to the righ.
        /// </summary>
        public void MoveBrickRight()
        {
            lock (this)
            {
                if (_brick != null)
                    _brick.MoveRight(_grid);
            }
        }

        /// <summary>
        /// Moves brick down.  Returns true if brick hits bottom.
        /// </summary>
        public void MoveBrickDown(int level, out bool hit, out bool resting)
        {
            lock (this)
            {
                hit = false;
                resting = false;
                if (_brick != null)
                    _brick.MoveDown(_grid, level, out hit, out resting);
            }
        }

        /// <summary>
        /// Rotates the live brick, if one exists.
        /// </summary>
        public void RotateBrick()
        {
            lock (this)
            {
                if (_brick != null)
                    _brick.Rotate(_grid);
            }
        }

        /// <summary>
        /// Takes the currently live brick and puts it in hold.  If there's already
        /// a brick held, the two swap.  Returns true if swap causes collision.
        /// </summary>
        public bool HoldBrick(int level)
        {
            lock (this)
            {
                if (_brick == null)
                    return false;

                int oX = _brick.X;
                int oY = _brick.Y;

                if (_hold == null)
                {
                    _hold = _brick;
                    //_hold.SetXY(0, 0);
                    SpawnBrick(level);
                    _brick.SetXY(oX, oY, _grid);
                    return Brick.Collision(_grid, _brick.Grid, _brick.X, _brick.Y);
                }
                else
                {
                    Brick temp = _hold;
                    _hold = _brick;
                    //_hold.SetXY(0, 0);
                    _brick = temp;
                    _brick.SetXY(oX, oY, _grid);
                    return Brick.Collision(_grid, _brick.Grid, _brick.X, _brick.Y);
                }
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
                        if (_grid[x, y] == 0)
                            solid = false;
                    if (solid)
                        rowsToErase.Add(y);
                }
                return rowsToErase;
            }
        }

    }
}
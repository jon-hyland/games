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
        private readonly Space[,] _grid;
        private readonly Queue<Brick> _nextBricks;
        private Brick _brick;
        private Brick _hold;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Matrix()
        {
            _random = new Random();
            _grid = new Space[12, 22];
            _nextBricks = new Queue<Brick>();
            _brick = null;
            _hold = null;
            NewGame(false);
        }

        /// <summary>
        /// XY class indexer that accesses the underlying grid, locking for thread safety.
        /// </summary>
        public Space this[int x, int y]
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
        public Space[,] GetGrid(bool includeBrick, bool includeGhost)
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
                ClearSpaces(_grid);
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
                if (spawnBrick)
                    SpawnBrick();
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
            lock (this)
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

        ///// <summary>
        ///// Converts 1D array of bytes to 2D array of spaces.
        ///// </summary>
        //public static Space[,] Bytes1DToSpaces(byte[] bytes, int width)
        //{
        //    if ((bytes.Length % width) != 0)
        //        throw new Exception("Invalid array length");
        //    int height = bytes.Length / width;
        //    Space[,] grid = new Space[width, height];
        //    Bytes1DToSpaces(bytes, grid);
        //    return grid;
        //}

        ///// <summary>
        ///// Converts 1D array of bytes to 2D array of spaces.
        ///// </summary>
        //public static void Bytes1DToSpaces(byte[] bytes, Space[,] grid)
        //{
        //    if (bytes.Length != grid.Length)
        //        throw new Exception("Invalid array length");
        //    for (int x = 0; x < grid.GetLength(0); x++)
        //        for (int y = 0; y < grid.GetLength(1); y++)
        //            grid[x, y] = (Space)bytes[x * y];
        //}

        ///// <summary>
        ///// Converts 2D array of spaces to 1D array of bytes.
        ///// </summary>
        //public static byte[] SpacesToBytes1D(Space[,] grid)
        //{
        //    byte[] bytes = new byte[grid.Length];
        //    SpacesToBytes1D(grid, bytes);
        //    return bytes;
        //}

        ///// <summary>
        ///// Converts 2D array of spaces to 1D array of bytes.
        ///// </summary>
        //public static void SpacesToBytes1D(Space[,] grid, byte[] bytes)
        //{
        //    if (bytes.Length != grid.Length)
        //        throw new Exception("Invalid array length");
        //    for (int x = 0; x < grid.GetLength(0); x++)
        //        for (int y = 0; y < grid.GetLength(1); y++)
        //            bytes[x * y] = (byte)grid[x, y];
        //}

        /// <summary>
        /// Fills grid with empty spaces.
        /// </summary>
        public static void ClearSpaces(Space[,] grid)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    grid[x, y] = Space.Empty;
        }

        /// <summary>
        /// Converts 2D array of spaces to 2D array of bytes.
        /// </summary>
        public static byte[,] SpacesToBytes(Space[,] grid)
        {
            byte[,] bytes = new byte[grid.GetLength(0), grid.GetLength(1)];
            SpacesToBytes(grid, bytes);
            return bytes;
        }

        /// <summary>
        /// Converts 2D array of spaces to 2D array of bytes.
        /// </summary>
        public static void SpacesToBytes(Space[,] grid, byte[,] bytes)
        {
            if ((bytes.GetLength(0) != grid.GetLength(0)) || (bytes.GetLength(1) != grid.GetLength(1)))
                throw new Exception("Invalid array length");
            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    bytes[x, y] = (byte)grid[x, y];
        }

        /// <summary>
        /// Converts 2D array of bytes to 2D array of spaces.
        /// </summary>
        public static Space[,] BytesToSpaces(byte[,] bytes)
        {
            Space[,] grid = new Space[bytes.GetLength(0), bytes.GetLength(1)];
            BytesToSpaces(bytes, grid);
            return grid;
        }

        /// <summary>
        /// Converts 2D array of bytes to 2D array of spaces.
        /// </summary>
        public static void BytesToSpaces(byte[,] bytes, Space[,] grid)
        {
            if ((bytes.GetLength(0) != grid.GetLength(0)) || (bytes.GetLength(1) != grid.GetLength(1)))
                throw new Exception("Invalid array length");
            for (int x = 0; x < bytes.GetLength(0); x++)
                for (int y = 0; y < bytes.GetLength(1); y++)
                    grid[x, y] = (Space)bytes[x, y];
        }

    }
}
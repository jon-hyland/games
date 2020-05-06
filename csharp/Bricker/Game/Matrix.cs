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

        //public
        public byte[,] Grid => _grid;
        public Brick Brick => _brick;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Matrix()
        {
            _random = new Random();
            _grid = new byte[12, 22];
            _nextBricks = new Queue<Brick>();
            _brick = null;
            NewGame(false);
        }

        /// <summary>
        /// Resets the game.
        /// </summary>
        public void NewGame(bool spawnBrick = true)
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
            if (spawnBrick)
                SpawnBrick();
        }

        /// <summary>
        /// Grabs the next brick in line, makes it the current brick.
        /// Refills the queue with six items.
        /// Returns true if current brick collides with matrix shape.
        /// </summary>
        public bool SpawnBrick()
        {
            lock (_nextBricks)
            {
                while (_nextBricks.Count < 7)
                    _nextBricks.Enqueue(new Brick(_random.Next(7) + 1));
                _brick = _nextBricks.Dequeue();
            }
            return _brick.Collision(_grid);
        }

        /// <summary>
        /// Moves resting brick to matrix.
        /// </summary>
        public void AddBrickToMatrix()
        {
            if (_brick == null)
                return;

            for (int x = 0; x < _brick.Width; x++)
                for (int y = 0; y < _brick.Height; y++)
                    if (_brick.Grid[x, y] > 0)
                        _grid[x + _brick.X, y + _brick.Y] = _brick.Grid[x, y];

            _brick = null;
        }

        /// <summary>
        /// Returns up to six next bricks.
        /// </summary>
        public Brick[] GetNextBricks()
        {
            lock (_nextBricks)
            {
                return _nextBricks.Take(6).ToArray();
            }
        }

        /// <summary>
        /// Moves brick to the left.
        /// </summary>
        public void MoveBrickLeft()
        {
            if (_brick != null)
                _brick.MoveLeft(_grid);
        }

        /// <summary>
        /// Moves brick to the righ.
        /// </summary>
        public void MoveBrickRight()
        {
            if (_brick != null)
                _brick.MoveRight(_grid);
        }

        /// <summary>
        /// Moves brick down.  Returns true if brick hits bottom.
        /// </summary>
        public bool MoveBrickDown()
        {
            bool hit = false;
            if (_brick != null)
                hit = _brick.MoveDown(_grid);
            return hit;
        }

        /// <summary>
        /// Rotates the live brick, if one exists.
        /// </summary>
        public void RotateBrick()
        {
            if (_brick != null)
                _brick.Rotate(_grid);
        }

        /// <summary>
        /// Returns list of all solid rows (ready to be cleared).
        /// </summary>
        public List<int> IdentifySolidRows()
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
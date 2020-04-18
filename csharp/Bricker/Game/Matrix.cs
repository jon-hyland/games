using Bricker.Rendering;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Bricker.Game
{
    /// <summary>
    /// Represents the 10x20 game matrix, game variables, and some game logic.
    /// </summary>
    public class Matrix
    {
        //private
        private readonly Random _random;
        private readonly int _width;
        private readonly int _height;
        private byte[,] _grid;
        private Brick _brick;
        private Brick _nextBrick;

        //public
        public int Width => _width;
        public int Height => _height;
        public byte[,] Grid => _grid;
        public Brick Brick => _brick;
        public Brick NextBrick => _nextBrick;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Matrix()
        {
            _random = new Random();
            _width = 12;
            _height = 22;            
            NewGame(false);
        }

        /// <summary>
        /// Resets the game.
        /// </summary>
        public void NewGame(bool spawnBrick = true)
        {
            _grid = new byte[_width, _height];
            for (int x = 0; x < _width; x++)
            {
                _grid[x, 0] = 8;
                _grid[x, _height - 1] = 8;
            }
            for (int y = 0; y < _height; y++)
            {
                _grid[0, y] = 8;
                _grid[_width - 1, y] = 8;
            }
            _brick = null;
            _nextBrick = null;
            if (spawnBrick)
                SpawnBrick();
        }

        /// <summary>
        /// Spawns a random new brick.  Returns true on collision (game over).
        /// </summary>
        public bool SpawnBrick()
        {
            if (_nextBrick == null)
                _nextBrick = new Brick(_random.Next(7) + 1);
            _brick = _nextBrick;
            _nextBrick = new Brick(_random.Next(7) + 1);
            return _nextBrick.Collision(_grid);
        }

        /// <summary>
        /// Moves resting brick to matrix.
        /// </summary>
        public void AddBrickToMatrix()
        {
            if (_brick != null)
            {
                for (int x = 0; x < _brick.Width; x++)
                {
                    for (int y = 0; y < _brick.Height; y++)
                    {
                        if (_brick.Grid[x, y] > 0)
                        {
                            _grid[x + _brick.X, y + _brick.Y] = _brick.Grid[x, y];
                        }
                    }
                }
            }
            _brick = null;
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
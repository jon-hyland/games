using Bricker.Rendering;
using SkiaSharp;
using System;

namespace Bricker.Game
{
    /// <summary>
    /// Represents a live, moving brick that has not yet joined the static game matrix.
    /// It will do so once it's hit bottom and come to rest.
    /// </summary>
    public class Brick
    {
        //private
        private readonly int _shapeNum;
        private readonly int _width;
        private readonly int _height;
        private readonly SKColor _color;

        private bool[,] _grid;
        private int _x;
        private int _y;
        private int _topSpace;
        private int _bottomSpace;
        private DateTime _lastDropTime;

        //public
        public int ShapeNum => _shapeNum;
        public int Width => _width;
        public int Height => _height;
        public SKColor Color => _color;
        public bool[,] Grid => _grid;
        public int X => _x;
        public int Y => _y;
        public int TopSpace => _topSpace;
        public int BottomSpace => _bottomSpace;
        public DateTime LastDropTime => _lastDropTime;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Brick(int shapeNum)
        {
            _shapeNum = shapeNum;
            switch (shapeNum)
            {
                case 1:
                    _width = 4;
                    _height = 4;
                    _grid = new bool[_width, _height];
                    _grid[0, 2] = true;
                    _grid[1, 2] = true;
                    _grid[2, 2] = true;
                    _grid[3, 2] = true;
                    _color = Colors.SilverPink;
                    break;
                case 2:
                    _width = 3;
                    _height = 3;
                    _grid = new bool[_width, _height];
                    _grid[0, 1] = true;
                    _grid[0, 2] = true;
                    _grid[1, 2] = true;
                    _grid[2, 2] = true;
                    _color = Colors.TuftsBlue;
                    break;
                case 3:
                    _width = 3;
                    _height = 3;
                    _grid = new bool[_width, _height];
                    _grid[2, 1] = true;
                    _grid[0, 2] = true;
                    _grid[1, 2] = true;
                    _grid[2, 2] = true;
                    _color = Colors.ChromeYellow;
                    break;
                case 4:
                    _width = 2;
                    _height = 2;
                    _grid = new bool[_width, _height];
                    _grid[0, 0] = true;
                    _grid[0, 1] = true;
                    _grid[1, 0] = true;
                    _grid[1, 1] = true;
                    _color = Colors.Independence;
                    break;
                case 5:
                    _width = 4;
                    _height = 4;
                    _grid = new bool[_width, _height];
                    _grid[1, 0] = true;
                    _grid[2, 0] = true;
                    _grid[0, 1] = true;
                    _grid[1, 1] = true;
                    _color = Colors.ForestGreen;
                    break;
                case 6:
                    _width = 4;
                    _height = 4;
                    _grid = new bool[_width, _height];
                    _grid[1, 1] = true;
                    _grid[0, 2] = true;
                    _grid[1, 2] = true;
                    _grid[2, 2] = true;
                    _color = Colors.Byzantine;
                    break;
                case 7:
                    _width = 4;
                    _height = 4;
                    _grid = new bool[_width, _height];
                    _grid[0, 0] = true;
                    _grid[1, 0] = true;
                    _grid[1, 1] = true;
                    _grid[2, 1] = true;
                    _color = Colors.Coquelicot;
                    break;
            }
            _topSpace = CalculateTopSpace();
            _bottomSpace = CalculateBottomSpace();
            _x = (12 - _width) / 2;
            _y = 1 - _topSpace;
            _lastDropTime = DateTime.Now;
        }

        /// <summary>
        /// Gets spacing at top of brick.
        /// </summary>
        private int CalculateTopSpace()
        {
            int topSpace = 0;
            for (int y = 0; y < _height; y++)
            {
                bool empty = true;
                for (int x = 0; x < _width; x++)
                {
                    if (_grid[x, y])
                        empty = false;
                }
                if (empty)
                    topSpace++;
                else
                    break;
            }
            return topSpace;
        }

        /// <summary>
        /// Gets spacing at bottom of brick.
        /// </summary>
        private int CalculateBottomSpace()
        {
            int bottomSpace = 0;
            for (int y = _height - 1; y >= 0; y--)
            {
                bool empty = true;
                for (int x = 0; x < _width; x++)
                {
                    if (_grid[x, y])
                        empty = false;
                }
                if (empty)
                    bottomSpace++;
                else
                    break;
            }
            return bottomSpace;
        }

        /// <summary>
        /// Returns true if filled brick space conflicts with filled matrix space.
        /// </summary>
        public bool Collision(bool[,] matrix)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_grid[x, y])
                    {
                        int mX = x + _x;
                        int mY = y + _y;
                        if ((mX < 0) || (mX > 21))
                            return true;
                        if ((mY < 0) || (mY > 21))
                            return true;
                        if (matrix[mX, mY])
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Moves the brick left, unless collision.
        /// </summary>
        public void MoveLeft(bool[,] matrix)
        {
            _x--;
            if (Collision(matrix))
                _x++;
        }

        /// <summary>
        /// Moves the brick right, unless collision.
        /// </summary>
        public void MoveRight(bool[,] matrix)
        {
            _x++;
            if (Collision(matrix))
                _x--;
        }

        /// <summary>
        /// Moves the brick down, unless collision.
        /// </summary>
        public bool MoveDown(bool[,] matrix)
        {
            _lastDropTime = DateTime.Now;
            _y++;
            if (Collision(matrix))
            {
                _y--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if its time to drop brick (gravity).
        /// </summary>
        public bool IsDropTime(double intervalMs)
        {
            double elapsedMs = (DateTime.Now - _lastDropTime).TotalMilliseconds;
            bool dropTime = elapsedMs >= intervalMs;
            return dropTime;
        }

        /// <summary>
        /// Rotates the brick 90* clockwise, moving slightly if there's a collision.
        /// </summary>
        public void Rotate(bool[,] matrix)
        {
            bool[,] newGrid = new bool[_height, _width];
            for (int x1 = 0; x1 < _width; x1++)
            {
                for (int y1 = 0; y1 < _height; y1++)
                {
                    int x2 = -y1 + (_height - 1);
                    int y2 = x1;
                    newGrid[x2, y2] = _grid[x1, y1];
                }
            }
            _grid = newGrid;

            int steps = 0;
            while (Collision(matrix))
            {
                _y++;
                steps++;
                if (steps >= 3)
                {
                    _y -= 3;
                    break;
                }
            }

            steps = 0;
            while (Collision(matrix))
            {
                _y--;
                steps++;
                if (steps >= 3)
                {
                    _y += 3;
                    break;
                }
            }

            steps = 0;
            while (Collision(matrix))
            {
                _x++;
                steps++;
                if (steps >= 3)
                {
                    _x -= 3;
                    break;
                }
            }

            steps = 0;
            while (Collision(matrix))
            {
                _x--;
                steps++;
                if (steps >= 3)
                {
                    _x += 3;
                    break;
                }
            }

            _topSpace = CalculateTopSpace();
            _bottomSpace = CalculateBottomSpace();
        }

    }
}

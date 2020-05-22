using Common.Standard.Error;
using Common.Standard.Logging;
using Common.Windows.Rendering;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bricker.Game
{
    /// <summary>
    /// Represents a live, moving brick that has not yet joined the static game matrix.
    /// It will do so once it's hit bottom and come to rest.
    /// </summary>
    public class Brick
    {
        //const
        private static readonly Dictionary<int, List<Point>> _antiCollisionPatterns = new Dictionary<int, List<Point>>();

        //private
        private readonly int _shapeNum;
        private readonly int _width;
        private readonly int _height;
        private readonly SKColor _color;
        private byte[,] _grid;
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
        public byte[,] Grid => _grid;
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
                    _grid = new byte[_width, _height];
                    _grid[0, 2] = 1;
                    _grid[1, 2] = 1;
                    _grid[2, 2] = 1;
                    _grid[3, 2] = 1;
                    _color = BrickToColor(1);
                    break;
                case 2:
                    _width = 3;
                    _height = 3;
                    _grid = new byte[_width, _height];
                    _grid[0, 1] = 2;
                    _grid[0, 2] = 2;
                    _grid[1, 2] = 2;
                    _grid[2, 2] = 2;
                    _color = BrickToColor(2);
                    break;
                case 3:
                    _width = 3;
                    _height = 3;
                    _grid = new byte[_width, _height];
                    _grid[2, 1] = 3;
                    _grid[0, 2] = 3;
                    _grid[1, 2] = 3;
                    _grid[2, 2] = 3;
                    _color = BrickToColor(3);
                    break;
                case 4:
                    _width = 2;
                    _height = 2;
                    _grid = new byte[_width, _height];
                    _grid[0, 0] = 4;
                    _grid[0, 1] = 4;
                    _grid[1, 0] = 4;
                    _grid[1, 1] = 4;
                    _color = BrickToColor(4);
                    break;
                case 5:
                    _width = 4;
                    _height = 4;
                    _grid = new byte[_width, _height];
                    _grid[1, 0] = 5;
                    _grid[2, 0] = 5;
                    _grid[0, 1] = 5;
                    _grid[1, 1] = 5;
                    _color = BrickToColor(5);
                    break;
                case 6:
                    _width = 4;
                    _height = 4;
                    _grid = new byte[_width, _height];
                    _grid[1, 1] = 6;
                    _grid[0, 2] = 6;
                    _grid[1, 2] = 6;
                    _grid[2, 2] = 6;
                    _color = BrickToColor(6);
                    break;
                case 7:
                    _width = 4;
                    _height = 4;
                    _grid = new byte[_width, _height];
                    _grid[0, 0] = 7;
                    _grid[1, 0] = 7;
                    _grid[1, 1] = 7;
                    _grid[2, 1] = 7;
                    _color = BrickToColor(7);
                    break;
            }
            _topSpace = CalculateTopSpace();
            _bottomSpace = CalculateBottomSpace();
            _x = (12 - _width) / 2;
            _y = 1 - _topSpace;
            _lastDropTime = DateTime.Now;
        }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Brick()
        {
            lock (_antiCollisionPatterns)
            {
                if (_antiCollisionPatterns.Count == 0)
                    _antiCollisionPatterns = GenerateAntiCollisionPatterns();
            }
        }

        /// <summary>
        /// Returns color for specified brick shape.
        /// </summary>
        public static SKColor BrickToColor(byte shape)
        {
            switch (shape)
            {
                //empty
                case 0:
                    return Colors.Black;

                //1
                case 1:
                    return Colors.SilverPink;

                //2
                case 2:
                    return Colors.TuftsBlue;

                //3
                case 3:
                    return Colors.ChromeYellow;

                //4
                case 4:
                    return Colors.Independence;

                //5
                case 5:
                    return Colors.ForestGreen;

                //6
                case 6:
                    return Colors.Byzantine;

                //7
                case 7:
                    return Colors.Coquelicot;

                //edge
                case 8:
                    return Colors.Transparent;

                //gray
                case 9:
                    return Colors.Gray;

                //undefined
                default:
                    return Colors.Transparent;
            }
        }

        /// <summary>
        /// Gets spacing at top of brick.
        /// </summary>
        private int CalculateTopSpace()
        {
            lock (this)
            {
                int topSpace = 0;
                for (int y = 0; y < _height; y++)
                {
                    bool empty = true;
                    for (int x = 0; x < _width; x++)
                    {
                        if (_grid[x, y] > 0)
                            empty = false;
                    }
                    if (empty)
                        topSpace++;
                    else
                        break;
                }
                return topSpace;
            }
        }

        /// <summary>
        /// Gets spacing at bottom of brick.
        /// </summary>
        private int CalculateBottomSpace()
        {
            lock (this)
            {
                int bottomSpace = 0;
                for (int y = _height - 1; y >= 0; y--)
                {
                    bool empty = true;
                    for (int x = 0; x < _width; x++)
                    {
                        if (_grid[x, y] > 0)
                            empty = false;
                    }
                    if (empty)
                        bottomSpace++;
                    else
                        break;
                }
                return bottomSpace;
            }
        }

        /// <summary>
        /// Moves the brick left, unless collision.
        /// </summary>
        public void MoveLeft(byte[,] matrix)
        {
            lock (this)
            {
                _x--;
                if (Collision(matrix, _grid, _x, _y))
                    _x++;
            }
        }

        /// <summary>
        /// Moves the brick right, unless collision.
        /// </summary>
        public void MoveRight(byte[,] matrix)
        {
            lock (this)
            {
                _x++;
                if (Collision(matrix, _grid, _x, _y))
                    _x--;
            }
        }

        /// <summary>
        /// Moves the brick down, unless collision.
        /// </summary>
        public bool MoveDown(byte[,] matrix)
        {
            lock (this)
            {
                _lastDropTime = DateTime.Now;
                _y++;
                if (Collision(matrix, _grid, _x, _y))
                {
                    _y--;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true if its time to drop brick (gravity).
        /// </summary>
        public bool IsDropTime(double intervalMs)
        {
            lock (this)
            {
                double elapsedMs = (DateTime.Now - _lastDropTime).TotalMilliseconds;
                bool dropTime = elapsedMs >= intervalMs;
                return dropTime;
            }
        }

        /// <summary>
        /// Rotates the brick 90* clockwise.
        /// </summary>
        public void Rotate(byte[,] matrixGrid)
        {
            lock (this)
            {
                byte[,] newBrickGrid = new byte[_height, _width];
                for (int x1 = 0; x1 < _width; x1++)
                {
                    for (int y1 = 0; y1 < _height; y1++)
                    {
                        int x2 = -y1 + (_height - 1);
                        int y2 = x1;
                        newBrickGrid[x2, y2] = _grid[x1, y1];
                    }
                }

                int newBrickX = _x, newBrickY = _y;
                PreventCollision(matrixGrid, newBrickGrid, ref newBrickX, ref newBrickY, 3);
                if (!Collision(matrixGrid, newBrickGrid, newBrickX, newBrickY))
                {
                    _grid = newBrickGrid;
                    _x = newBrickX;
                    _y = newBrickY;
                }

                _topSpace = CalculateTopSpace();
                _bottomSpace = CalculateBottomSpace();
            }
        }

        /// <summary>
        /// Returns true if filled brick space conflicts with filled matrix space.
        /// </summary>
        public static bool Collision(byte[,] matrixGrid, byte[,] brickGrid, int brickX, int brickY)
        {
            try
            {
                for (int x = 0; x < brickGrid.GetLength(0); x++)
                {
                    for (int y = 0; y < brickGrid.GetLength(1); y++)
                    {
                        if (brickGrid[x, y] > 0)
                        {
                            int matrixX = x + brickX;
                            int matrixY = y + brickY;
                            if ((matrixX < 0) || (matrixX > 21))
                                return true;
                            if ((matrixY < 0) || (matrixY > 21))
                                return true;
                            if (matrixGrid[matrixX, matrixY] > 0)
                                return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                try
                {
                    byte[] mg = new byte[matrixGrid.GetLength(0) * matrixGrid.GetLength(1)];
                    Buffer.BlockCopy(matrixGrid, 0, mg, 0, mg.Length);
                    byte[] bg = new byte[brickGrid.GetLength(0) * brickGrid.GetLength(1)];
                    Buffer.BlockCopy(brickGrid, 0, bg, 0, bg.Length);
                    Log.Write($"bx: {brickX}, by: {brickY}, mg: {String.Join("", mg.Select(x => x.ToString()))}, bg: {String.Join("", bg.Select(x => x.ToString()))}");
                    ErrorHandler.LogError(ex);
                }
                catch
                {
                }
                return true;
            }
        }

        /// <summary>
        /// Tries moving brick up to X steps in any direction to avoid a collision.  If not,
        /// brick remains at original location.
        /// </summary>
        private static void PreventCollision(byte[,] matrixGrid, byte[,] brickGrid, ref int brickX, ref int brickY, int maxSteps)
        {
            int oX = brickX, oY = brickY;
            try
            {
                maxSteps = Math.Max(Math.Min(maxSteps, 5), 1);
                List<Point> pattern;
                lock (_antiCollisionPatterns)
                {
                    pattern = _antiCollisionPatterns[maxSteps];
                }

                int newX, newY;
                for (int i = 0; i < pattern.Count; i++)
                {
                    newX = brickX + pattern[i].X;
                    newY = brickY + pattern[i].Y;
                    if (!Collision(matrixGrid, brickGrid, newX, newY))
                    {
                        brickX = newX;
                        brickY = newY;
                        return;
                    }
                }
            }
            finally
            {
                if ((brickX != oX) || (brickY != oY))
                {
                    string foo = "";
                }
            }
        }

        /// <summary>
        /// Changes the X/Y location, usually on hold swap.
        /// </summary>
        public void SetXY(int x, int y, byte[,] matrixGrid)
        {
            lock (this)
            {
                PreventCollision(matrixGrid, _grid, ref x, ref y, 5);
                if (!Collision(matrixGrid, _grid, x, y))
                {
                    _x = x;
                    _y = y;
                }
            }
        }

        /// <summary>
        /// Called when spawned onto the matrix, used to reset last drop time, etc.
        /// </summary>
        public void Spawned()
        {
            _lastDropTime = DateTime.Now;
        }

        /// <summary>
        /// Returns copy of brick.
        /// </summary>
        public Brick Clone()
        {
            lock (this)
            {
                Brick brick = new Brick(_shapeNum)
                {
                    _grid = (byte[,])_grid.Clone(),
                    _x = _x,
                    _y = _y,
                    _topSpace = _topSpace,
                    _bottomSpace = _bottomSpace,
                    _lastDropTime = _lastDropTime
                };
                return brick;
            }
        }

        /// <summary>
        /// Pregenerates five anti-collion patterns, one for each 1-5 max steps.
        /// </summary>
        private static Dictionary<int, List<Point>> GenerateAntiCollisionPatterns()
        {
            Dictionary<int, List<Point>> patterns = new Dictionary<int, List<Point>>();
            for (int maxSteps = 1; maxSteps <= 5; maxSteps++)
            {
                List<Point> pattern = GeneratePattern(maxSteps);
                patterns.Add(maxSteps, pattern);
            }
            return patterns;
        }

        /// <summary>
        /// Generstes an anti-collision pattern for specified number of max steps.
        /// </summary>
        private static List<Point> GeneratePattern(int maxSteps)
        {
            List<Point> pattern = new List<Point>();
            pattern.Add(new Point(0, 0));

            for (int s = 1; s <= maxSteps; s++)
                pattern.Add(new Point(s, 0));
            for (int s = 1; s <= maxSteps; s++)
                pattern.Add(new Point(-s, 0));
            for (int s = 1; s <= maxSteps; s++)
                pattern.Add(new Point(0, s));
            for (int s = 1; s <= maxSteps; s++)
                pattern.Add(new Point(0, -s));

            int x = 1, y = 0;
            pattern.Add(new Point(x, y));

            for (int maxStep = 1; maxStep <= maxSteps; maxStep++)
            {
                while (y < maxStep)
                {
                    y++;
                    pattern.Add(new Point(x, y));
                }
                while (x > -maxStep)
                {
                    x--;
                    pattern.Add(new Point(x, y));
                }
                while (y > -maxStep)
                {
                    y--;
                    pattern.Add(new Point(x, y));
                }
                while (x < maxStep)
                {
                    x++;
                    pattern.Add(new Point(x, y));
                }
                while (y <= -2)
                {
                    y++;
                    pattern.Add(new Point(x, y));
                }
                x++;
            }

            pattern = pattern.Distinct().ToList();
            return pattern;
        }

        /// <summary>
        /// Represents an XY point.
        /// </summary>
        private struct Point
        {
            public int X { get; set; }
            public int Y { get; set; }

            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public override string ToString()
            {
                return $"{X}, {Y}";
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + X.GetHashCode();
                    hash = hash * 31 + Y.GetHashCode();
                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Point p))
                    return false;
                return GetHashCode() == p.GetHashCode();
            }
        }

    }
}

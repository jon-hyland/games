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
        private static readonly object _staticLock = new object();
        private static readonly Dictionary<int, List<Point>> _antiCollisionPatterns;

        //private
        private readonly Space _space;
        private readonly int _width;
        private readonly int _height;
        private readonly SKColor _color;
        private Space[,] _grid;
        private int _x;
        private int _y;
        private int _yGhost;
        private int _topSpace;
        private int _bottomSpace;
        private DateTime _lastDropTime;

        //public
        public Space Space => _space;
        public int Width => _width;
        public int Height => _height;
        public SKColor Color => _color;
        public Space[,] Grid => _grid;
        public int X => _x;
        public int Y => _y;
        public int YGhost => _yGhost;
        public int TopSpace => _topSpace;
        public int BottomSpace => _bottomSpace;
        public DateTime LastDropTime => _lastDropTime;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Brick(Space space)
        {
            _space = space;
            switch (space)
            {
                case Space.I_White:
                    _width = 4;
                    _height = 4;
                    _grid = new Space[_width, _height];
                    _grid[0, 2] = space;
                    _grid[1, 2] = space;
                    _grid[2, 2] = space;
                    _grid[3, 2] = space;
                    _color = SpaceToColor(space);
                    break;
                case Space.J_Blue:
                    _width = 3;
                    _height = 3;
                    _grid = new Space[_width, _height];
                    _grid[0, 1] = space;
                    _grid[0, 2] = space;
                    _grid[1, 2] = space;
                    _grid[2, 2] = space;
                    _color = SpaceToColor(space);
                    break;
                case Space.L_Yellow:
                    _width = 3;
                    _height = 3;
                    _grid = new Space[_width, _height];
                    _grid[2, 1] = space;
                    _grid[0, 2] = space;
                    _grid[1, 2] = space;
                    _grid[2, 2] = space;
                    _color = SpaceToColor(space);
                    break;
                case Space.O_Gray:
                    _width = 2;
                    _height = 2;
                    _grid = new Space[_width, _height];
                    _grid[0, 0] = space;
                    _grid[0, 1] = space;
                    _grid[1, 0] = space;
                    _grid[1, 1] = space;
                    _color = SpaceToColor(space);
                    break;
                case Space.S_Green:
                    _width = 4;
                    _height = 4;
                    _grid = new Space[_width, _height];
                    _grid[1, 0] = space;
                    _grid[2, 0] = space;
                    _grid[0, 1] = space;
                    _grid[1, 1] = space;
                    _color = SpaceToColor(space);
                    break;
                case Space.T_Purple:
                    _width = 4;
                    _height = 4;
                    _grid = new Space[_width, _height];
                    _grid[1, 1] = space;
                    _grid[0, 2] = space;
                    _grid[1, 2] = space;
                    _grid[2, 2] = space;
                    _color = SpaceToColor(space);
                    break;
                case Space.Z_Red:
                    _width = 4;
                    _height = 4;
                    _grid = new Space[_width, _height];
                    _grid[0, 0] = space;
                    _grid[1, 0] = space;
                    _grid[1, 1] = space;
                    _grid[2, 1] = space;
                    _color = SpaceToColor(space);
                    break;
            }
            _topSpace = CalculateTopSpace();
            _bottomSpace = CalculateBottomSpace();
            _x = (12 - _width) / 2;
            _y = 1 - _topSpace;
            _yGhost = _y;  //fix
            _lastDropTime = DateTime.Now;
        }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Brick()
        {
            lock (_staticLock)
            {
                _antiCollisionPatterns = GenerateAntiCollisionPatterns();
            }
        }

        /// <summary>
        /// Returns color for specified brick shape.
        /// </summary>
        public static SKColor SpaceToColor(Space space)
        {
            switch (space)
            {
                case Space.Empty:
                    return Colors.Transparent;
                case Space.I_White:
                    return Colors.SilverPink;
                case Space.J_Blue:
                    return Colors.TuftsBlue;
                case Space.L_Yellow:
                    return Colors.ChromeYellow;
                case Space.O_Gray:
                    return Colors.Independence;
                case Space.S_Green:
                    return Colors.ForestGreen;
                case Space.T_Purple:
                    return Colors.Byzantine;
                case Space.Z_Red:
                    return Colors.Coquelicot;
                case Space.Edge:
                    return Colors.Transparent;
                case Space.I_White_Ghost:
                    return Colors.GetMuchDarker(Colors.SilverPink);
                case Space.J_Blue_Ghost:
                    return Colors.GetMuchDarker(Colors.TuftsBlue);
                case Space.L_Yellow_Ghost:
                    return Colors.GetMuchDarker(Colors.ChromeYellow);
                case Space.O_Gray_Ghost:
                    return Colors.GetMuchDarker(Colors.Independence);
                case Space.S_Green_Ghost:
                    return Colors.GetMuchDarker(Colors.ForestGreen);
                case Space.T_Purple_Ghost:
                    return Colors.GetMuchDarker(Colors.Byzantine);
                case Space.Z_Red_Ghost:
                    return Colors.GetMuchDarker(Colors.Coquelicot);
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
                        if (_grid[x, y].IsSolid())
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
                        if (_grid[x, y].IsSolid())
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
        public void MoveLeft(Space[,] matrixGrid, out bool moved, out bool resting)
        {
            lock (this)
            {
                _x--;
                moved = true;
                if (Collision(matrixGrid, _grid, _x, _y))
                {
                    _x++;
                    moved = false;
                }
                resting = Resting(matrixGrid, _grid, _x, _y);
                _yGhost = GetYGhost(matrixGrid, _grid, _x, _y);
            }
        }

        /// <summary>
        /// Moves the brick right, unless collision.
        /// </summary>
        public void MoveRight(Space[,] matrixGrid, out bool moved, out bool resting)
        {
            lock (this)
            {
                _x++;
                moved = true;
                if (Collision(matrixGrid, _grid, _x, _y))
                {
                    _x--;
                    moved = false;
                }
                resting = Resting(matrixGrid, _grid, _x, _y);
                _yGhost = GetYGhost(matrixGrid, _grid, _x, _y);
            }
        }

        /// <summary>
        /// Moves the brick down, unless collision.
        /// </summary>
        public void MoveDown(Space[,] matrixGrid, out bool hit, out bool resting)
        {
            lock (this)
            {
                hit = false;
                resting = false;
                _lastDropTime = DateTime.Now;
                _y++;
                if (Collision(matrixGrid, _grid, _x, _y))
                {
                    _y--;
                    hit = true;
                }
                if (Resting(matrixGrid, _grid, _x, _y))
                {
                    resting = true;
                }
                _yGhost = GetYGhost(matrixGrid, _grid, _x, _y);
            }
        }

        /// <summary>
        /// Rotates the brick 90* clockwise.
        /// </summary>
        public void Rotate(Space[,] matrixGrid, out bool resting)
        {
            lock (this)
            {
                Space[,] newBrickGrid = new Space[_height, _width];
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
                resting = Resting(matrixGrid, _grid, _x, _y);

                _yGhost = GetYGhost(matrixGrid, _grid, _x, _y);
                _topSpace = CalculateTopSpace();
                _bottomSpace = CalculateBottomSpace();
            }
        }

        /// <summary>
        /// Returns true if a filled brick space is above a filled grid space.
        /// </summary>
        public static bool Resting(Space[,] matrixGrid, Space[,] brickGrid, int brickX, int brickY)
        {
            try
            {
                for (int x = 0; x < brickGrid.GetLength(0); x++)
                {
                    for (int y = 0; y < brickGrid.GetLength(1); y++)
                    {
                        if (brickGrid[x, y].IsSolid())
                        {
                            int matrixX = x + brickX;
                            int matrixY = y + brickY + 1;
                            if ((matrixX < 0) || (matrixX > 21))
                                continue;
                            if ((matrixY < 0) || (matrixY > 21))
                                continue;
                            if (matrixGrid[matrixX, matrixY].IsSolid())
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
                    Space[] mg = new Space[matrixGrid.GetLength(0) * matrixGrid.GetLength(1)];
                    Buffer.BlockCopy(matrixGrid, 0, mg, 0, mg.Length);
                    Space[] bg = new Space[brickGrid.GetLength(0) * brickGrid.GetLength(1)];
                    Buffer.BlockCopy(brickGrid, 0, bg, 0, bg.Length);
                    Log.Write($"bx: {brickX}, by: {brickY}, mg: {String.Join("", mg.Select(x => ((byte)x).ToString()))}, bg: {String.Join("", bg.Select(x => ((byte)x).ToString()))}");
                    ErrorHandler.LogError(ex);
                }
                catch
                {
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true if filled brick space conflicts with filled matrix space.
        /// </summary>
        public static bool Collision(Space[,] matrixGrid, Space[,] brickGrid, int brickX, int brickY)
        {
            try
            {
                for (int x = 0; x < brickGrid.GetLength(0); x++)
                {
                    for (int y = 0; y < brickGrid.GetLength(1); y++)
                    {
                        if (brickGrid[x, y].IsSolid())
                        {
                            int matrixX = x + brickX;
                            int matrixY = y + brickY;
                            if ((matrixX < 0) || (matrixX > 21))
                                return true;
                            if ((matrixY < 0) || (matrixY > 21))
                                return true;
                            if (matrixGrid[matrixX, matrixY].IsSolid())
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
                    Space[] mg = new Space[matrixGrid.GetLength(0) * matrixGrid.GetLength(1)];
                    Buffer.BlockCopy(matrixGrid, 0, mg, 0, mg.Length);
                    Space[] bg = new Space[brickGrid.GetLength(0) * brickGrid.GetLength(1)];
                    Buffer.BlockCopy(brickGrid, 0, bg, 0, bg.Length);
                    Log.Write($"bx: {brickX}, by: {brickY}, mg: {String.Join("", mg.Select(x => ((byte)x).ToString()))}, bg: {String.Join("", bg.Select(x => ((byte)x).ToString()))}");
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
        private static void PreventCollision(Space[,] matrixGrid, Space[,] brickGrid, ref int brickX, ref int brickY, int maxSteps)
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

        /// <summary>
        /// Changes the X/Y location, usually on hold swap.
        /// </summary>
        public void SetXY(int x, int y, Space[,] matrixGrid)
        {
            lock (this)
            {
                PreventCollision(matrixGrid, _grid, ref x, ref y, 5);
                if (!Collision(matrixGrid, _grid, x, y))
                {
                    _x = x;
                    _y = y;
                }
                _yGhost = GetYGhost(matrixGrid, _grid, _x, _y);
            }
        }

        /// <summary>
        /// Calculates Y ghost (y of brick at rest).
        /// </summary>
        private static int GetYGhost(Space[,] matrixGrid, Space[,] brickGrid, int brickX, int brickY)
        {
            int lastGoodY = brickY;
            for (int y = brickY; y < matrixGrid.GetLength(1); y++)
            {
                if (!Collision(matrixGrid, brickGrid, brickX, y))
                    lastGoodY = y;
                else
                    break;
            }
            return lastGoodY;
        }

        /// <summary>
        /// Called when spawned onto the matrix, used to reset last drop time, calculate ghost, etc.
        /// </summary>
        public void Spawned(Space[,] matrixGrid)
        {
            _lastDropTime = DateTime.Now;
            _yGhost = GetYGhost(matrixGrid, _grid, _x, _y);
        }

        /// <summary>
        /// Returns copy of brick.
        /// </summary>
        public Brick Clone()
        {
            lock (this)
            {
                Brick brick = new Brick(_space)
                {
                    _grid = (Space[,])_grid.Clone(),
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

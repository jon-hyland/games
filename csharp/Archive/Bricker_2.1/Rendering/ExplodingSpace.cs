using SkiaSharp;
using System;

namespace Bricker.Rendering
{
    /// <summary>
    /// Represents an exploding matrix space, used on game over.
    /// </summary>
    public class ExplodingSpace
    {
        //private
        private static readonly Random _random = new Random();
        private double _x;
        private double _y;
        private readonly SKColor _color;
        private readonly double _xVelocity;
        private readonly double _yVelocity;

        //public
        public double X { get { return _x; } set { _x = value; } }
        public double Y { get { return _y; } set { _y = value; } }
        public SKColor Color => _color;
        public double XVelocity => _xVelocity;
        public double YVelocity => _yVelocity;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public ExplodingSpace(double x, double y, SKColor color)
        {
            _x = x;
            _y = y;
            _color = color;
            _xVelocity = (_random.NextDouble() * 100) + 10;
            _yVelocity = (_random.NextDouble() * 100) + 10;
            if (_random.Next(2) == 1)
                _xVelocity = -_xVelocity;
            if (_random.Next(2) == 1)
                _yVelocity = -_yVelocity;
        }
    }
}

using Bricker.Game;
using SkiaSharp;
using System;

namespace Bricker.Rendering
{
    /// <summary>
    /// Represents an exploding matrix space, used on game over.
    /// </summary>
    public class BackgroundTile
    {
        //private
        private static readonly Random _random = new Random();
        private double _x;
        private double _y;
        private readonly double _size;
        private readonly SKColor _color;
        private double _xVelocity;
        private double _yVelocity;
        private DateTime _lastMove;

        //public
        public double X { get { return _x; } set { _x = value; } }
        public double Y { get { return _y; } set { _y = value; } }
        public double Size => _size;
        public SKColor Color => _color;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public BackgroundTile()
        {
            _x = _random.NextDouble() * 1200;
            _y = _random.NextDouble() * 700;
            _size = (_random.NextDouble() * 150) + 50;
            _color = Brick.BrickToColor((byte)(_random.Next(8) + 1));
            _color = new SKColor(_color.Red, _color.Green, _color.Blue, 50);
            _xVelocity = (_random.NextDouble() * 100) + 10;
            _yVelocity = (_random.NextDouble() * 100) + 10;
            if (_random.Next(2) == 1)
                _xVelocity = -_xVelocity;
            if (_random.Next(2) == 1)
                _yVelocity = -_yVelocity;
            _lastMove = DateTime.Now;
        }

        /// <summary>
        /// Moves tile.
        /// </summary>
        public void Move(DateTime now, double level)
        {
            TimeSpan elapsed = now - _lastMove;
            _x += _xVelocity * level * elapsed.TotalSeconds;
            _y += _yVelocity * level * elapsed.TotalSeconds;
            if ((_x < -100) || (_x > 1300))
                _xVelocity = -_xVelocity;
            if ((_y < -100) || (_y > 800))
                _yVelocity = -_yVelocity;
            _lastMove = now;
        }
    }
}

using Bricker.Game;
using SkiaSharp;
using System;

namespace Bricker.Rendering.Tiles
{
    /// <summary>
    /// A solid color background tile.
    /// </summary>
    public class SolidTile : ITile
    {
        //private
        private static readonly Random _random = new Random();
        private double _x;
        private double _y;
        private double _width;
        private double _height;
        private SKColor _color;
        private double _xVelocity;
        private double _yVelocity;
        private DateTime _lastMove;

        //public
        public double X => _x;
        public double Y => _y;
        public double Width => _width;
        public double Height => _height;
        public SKColor Color => _color;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public SolidTile()
        {
            _x = _random.NextDouble() * 1200;
            _y = _random.NextDouble() * 700;
            _width = (_random.NextDouble() * 150) + 50;
            _height = (_random.NextDouble() * 150) + 50;
            _color = Brick.SpaceToColor((Space)(_random.Next(7) + 1));
            _color = new SKColor(_color.Red, _color.Green, _color.Blue, 50);
            _xVelocity = (_random.NextDouble() * 75) + 5;
            _yVelocity = (_random.NextDouble() * 75) + 5;
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
            _x += _xVelocity * (level * 0.75d) * elapsed.TotalSeconds;
            _y += _yVelocity * (level * 0.75d) * elapsed.TotalSeconds;
            if ((_x < -150) || (_x > 1350))
            {
                _xVelocity = -_xVelocity;
                _width = (_random.NextDouble() * 150) + 50;
                _height = (_random.NextDouble() * 150) + 50;
                _color = Brick.SpaceToColor((Space)(_random.Next(7) + 1));
                _color = new SKColor(_color.Red, _color.Green, _color.Blue, 50);
            }
            if ((_y < -150) || (_y > 850))
            {
                _yVelocity = -_yVelocity;
                _width = (_random.NextDouble() * 150) + 50;
                _height = (_random.NextDouble() * 150) + 50;
                _color = Brick.SpaceToColor((Space)(_random.Next(7) + 1));
                _color = new SKColor(_color.Red, _color.Green, _color.Blue, 50);
            }
            _lastMove = now;
        }




    }
}

using Common.Rendering;
using Common.Windows.Rendering;
using SkiaSharp;
using System;

namespace Bricker.Rendering.Tiles
{
    /// <summary>
    /// An image-based background tile.
    /// </summary>
    public class ImageTile : ITile
    {
        //private
        private static readonly Random _random = new Random();
        private readonly Images _images;
        private double _x;
        private double _y;
        private double _width;
        private double _height;
        private Image _image;
        private double _xVelocity;
        private double _yVelocity;
        private DateTime _lastMove;

        //public
        public double X => _x;
        public double Y => _y;
        public double Width => _width;
        public double Height => _height;
        public Image Image => _image;
        public SKColor Color => Colors.Transparent;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public ImageTile(Images images)
        {
            _images = images;
            _x = _random.NextDouble() * 1200;
            _y = _random.NextDouble() * 700;
            _image = _images.GetRandomImageFromCache();
            _width = _image?.Width ?? 1;
            _height = _image?.Height ?? 1;
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
            if ((_x < -300) || (_x > 1500))
            {
                _xVelocity = -_xVelocity;
                _image = _images.GetRandomImageFromCache();
                _width = _image.Width;
                _height = _image.Height;
            }
            if ((_y < -300) || (_y > 1000))
            {
                _yVelocity = -_yVelocity;
                _image = _images.GetRandomImageFromCache();
                _width = _image.Width;
                _height = _image.Height;
            }
            _lastMove = now;
        }




    }
}

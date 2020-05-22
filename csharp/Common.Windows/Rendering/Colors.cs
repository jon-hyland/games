using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Common.Windows.Rendering
{
    /// <summary>
    /// Easy access to selected game colors.
    /// </summary>
    public static class Colors
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<SKColor, SKColor> _lighter = new Dictionary<SKColor, SKColor>();
        private static readonly Dictionary<SKColor, SKColor> _darker = new Dictionary<SKColor, SKColor>();
        private static readonly Dictionary<SKColor, SKColor> _muchDarker = new Dictionary<SKColor, SKColor>();

        private static readonly SKColor _transparent;
        private static readonly SKColor _black;
        private static readonly SKColor _errorBlack;
        private static readonly SKColor _debugBlack1;
        private static readonly SKColor _debugBlack2;
        private static readonly SKColor _dimWhite;
        private static readonly SKColor _white;
        private static readonly SKColor _gray;
        private static readonly SKColor _pansyPurple;
        private static readonly SKColor _pinkRaspberry;
        private static readonly SKColor _vividCrimson;
        private static readonly SKColor _portlandOrange;
        private static readonly SKColor _fluorescentOrange;
        private static readonly SKColor _silverPink;
        private static readonly SKColor _independence;
        private static readonly SKColor _coquelicot;
        private static readonly SKColor _chromeYellow;
        private static readonly SKColor _byzantine;
        private static readonly SKColor _forestGreen;
        private static readonly SKColor _tuftsBlue;
        private static readonly SKColor _alphaBlack64;
        private static readonly SKColor _alphaBlack128;
        private static readonly SKColor _alphaBlack192;

        public static SKColor Transparent => _transparent;
        public static SKColor Black => _black;
        public static SKColor ErrorBlack => _errorBlack;
        public static SKColor DebugBlack1 => _debugBlack1;
        public static SKColor DebugBlack2 => _debugBlack2;
        public static SKColor DimWhite => _dimWhite;
        public static SKColor White => _white;
        public static SKColor Gray => _gray;
        public static SKColor PansyPurple => _pansyPurple;
        public static SKColor PinkRaspberry => _pinkRaspberry;
        public static SKColor VividCrimson => _vividCrimson;
        public static SKColor PortlandOrange => _portlandOrange;
        public static SKColor FluorescentOrange => _fluorescentOrange;
        public static SKColor SilverPink => _silverPink;
        public static SKColor Independence => _independence;
        public static SKColor Coquelicot => _coquelicot;
        public static SKColor ChromeYellow => _chromeYellow;
        public static SKColor Byzantine => _byzantine;
        public static SKColor ForestGreen => _forestGreen;
        public static SKColor TuftsBlue => _tuftsBlue;
        public static SKColor AlphaBlack64 => _alphaBlack64;
        public static SKColor AlphaBlack128 => _alphaBlack128;
        public static SKColor AlphaBlack192 => _alphaBlack192;

        static Colors()
        {
            _transparent = new SKColor(0, 0, 0, 0);
            _black = new SKColor(0, 0, 0);
            _errorBlack = new SKColor(50, 0, 0);
            _debugBlack1 = new SKColor(25, 0, 0);
            _debugBlack2 = new SKColor(50, 0, 0);
            _dimWhite = new SKColor(160, 152, 145);
            _white = new SKColor(240, 230, 220);
            _gray = new SKColor(25, 25, 25);
            _pansyPurple = new SKColor(87, 24, 69);
            _pinkRaspberry = new SKColor(144, 12, 62);
            _vividCrimson = new SKColor(199, 0, 57);
            _portlandOrange = new SKColor(255, 87, 51);
            _fluorescentOrange = new SKColor(255, 195, 0);
            _silverPink = new SKColor(196, 187, 175);
            _independence = new SKColor(73, 88, 103);
            _coquelicot = new SKColor(252, 49, 0);
            _chromeYellow = new SKColor(243, 169, 3);
            _byzantine = new SKColor(170, 56, 168);
            _forestGreen = new SKColor(54, 137, 38);
            _tuftsBlue = new SKColor(74, 125, 219);
            _alphaBlack64 = new SKColor(0, 0, 0, 64);
            _alphaBlack128 = new SKColor(0, 0, 0, 128);
            _alphaBlack192 = new SKColor(0, 0, 0, 192);
        }

        public static SKColor GetLighter(SKColor color)
        {
            lock (_lock)
            {
                if (!_lighter.ContainsKey(color))
                    _lighter.Add(color, new SKColor(Multiply(color.Red, 1.2), Multiply(color.Green, 1.2), Multiply(color.Blue, 1.2), color.Alpha));
                return _lighter[color];
            }
        }

        public static SKColor GetDarker(SKColor color)
        {
            lock (_lock)
            {
                if (!_darker.ContainsKey(color))
                    _darker.Add(color, new SKColor(Multiply(color.Red, 0.8), Multiply(color.Green, 0.8), Multiply(color.Blue, 0.8)));
                return _darker[color];
            }
        }

        public static SKColor GetMuchDarker(SKColor color)
        {
            lock (_lock)
            {
                if (!_muchDarker.ContainsKey(color))
                    _muchDarker.Add(color, new SKColor(Multiply(color.Red, 0.25), Multiply(color.Green, 0.25), Multiply(color.Blue, 0.25)));
                return _muchDarker[color];
            }
        }

        private static byte Multiply(byte value, double multiplier)
        {
            double result = value * multiplier;
            return (byte)Math.Min(Math.Max(result, 0), 255);
        }

    }
}


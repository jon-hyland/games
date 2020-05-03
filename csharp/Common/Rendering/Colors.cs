using SkiaSharp;

namespace Common.Rendering
{
    /// <summary>
    /// Easy access to selected game colors.
    /// </summary>
    public static class Colors
    {
        private static readonly SKColor _transparent;
        private static readonly SKColor _black;
        private static readonly SKColor _errorBlack;
        private static readonly SKColor _debugBlack1;
        private static readonly SKColor _debugBlack2;
        private static readonly SKColor _white;
        private static readonly SKColor _brightWhite;
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

        public static SKColor Transparent => _transparent;
        public static SKColor Black => _black;
        public static SKColor ErrorBlack => _errorBlack;
        public static SKColor DebugBlack1 => _debugBlack1;
        public static SKColor DebugBlack2 => _debugBlack2;
        public static SKColor White => _white;
        public static SKColor BrightWhite => _brightWhite;
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

        static Colors()
        {
            _transparent = new SKColor(0, 0, 0, 0);
            _black = new SKColor(0, 0, 0);
            _errorBlack = new SKColor(50, 0, 0);
            _debugBlack1 = new SKColor(25, 0, 0);
            _debugBlack2 = new SKColor(50, 0, 0);
            _white = new SKColor(196, 187, 175);
            _brightWhite = new SKColor(240, 230, 220);
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
        }

    }
}


using Common.Standard.Configuration;
using SkiaSharp;

namespace Common.Windows.Rendering
{
    public static class RenderProps
    {
        private static readonly object _lock = new object();
        private static IGameConfig _config;
        private static SKTypeface _typeface;
        private static SKPaint _linePaint;
        private static SKPaint _rectPaint;
        private static SKPaint _textPaint;
        private static double _displayScale;
        private static bool _scaleSet;

        public static bool AntiAlias { get; set; }
        public static bool HighFrameRate { get; set; }
        public static bool Background { get; set; }
        public static bool Debug { get; set; }
        public static double DisplayScale { get { return _displayScale; } set { _displayScale = value; _scaleSet = true; } }
        public static bool ScaleSet => _scaleSet;

        public static SKTypeface Typeface
        {
            get
            {
                lock (_lock)
                {
                    if (_typeface == null)
                        _typeface = SKTypeface.FromFile(_config.FontFile);
                    return _typeface;
                }
            }
            set
            {
                lock (_lock)
                    _typeface = value;
            }
        }

        public static SKPaint LinePaint
        {
            get
            {
                lock (_lock)
                {
                    if (_linePaint == null)
                        _linePaint = new SKPaint()
                        {
                            Color = Colors.White,
                            Style = SKPaintStyle.Stroke,
                            StrokeCap = SKStrokeCap.Square,
                            StrokeJoin = SKStrokeJoin.Bevel,
                            StrokeWidth = (float)(2 * DisplayScale),
                            IsAntialias = AntiAlias
                        };
                    return _linePaint;
                }
            }
            set
            {
                lock (_lock)
                    _linePaint = value;
            }
        }

        public static SKPaint RectPaint
        {
            get
            {
                lock (_lock)
                {
                    if (_rectPaint == null)
                        _rectPaint = new SKPaint()
                        {
                            Color = Colors.White,
                            Style = SKPaintStyle.StrokeAndFill,
                            StrokeCap = SKStrokeCap.Square,
                            StrokeJoin = SKStrokeJoin.Bevel,
                            IsAntialias = AntiAlias
                        };
                    return _rectPaint;
                }
            }
            set
            {
                lock (_lock)
                    _rectPaint = value;
            }
        }

        public static SKPaint TextPaint
        {
            get
            {
                lock (_lock)
                {
                    if (_textPaint == null)
                        _textPaint = _textPaint ?? new SKPaint()
                        {
                            Color = Colors.White,
                            Typeface = Typeface,
                            TextSize = (float)(12 * DisplayScale),
                            IsStroke = false,
                            IsAntialias = AntiAlias
                        };
                    return _textPaint;
                }
            }
            set
            {
                lock (_lock)
                    _textPaint = value;
            }
        }

        public static void Initialize(IGameConfig config)
        {
            _config = config;
            AntiAlias = config.AntiAlias;
            HighFrameRate = config.HighFrameRate;
            Background = config.Background;
            Debug = config.Debug;
            _displayScale = 1d;
        }

        public static void ResetSkia()
        {
            lock (_lock)
            {
                _typeface?.Dispose();
                _linePaint?.Dispose();
                _rectPaint?.Dispose();
                _textPaint?.Dispose();
                _typeface = null;
                _linePaint = null;
                _rectPaint = null;
                _textPaint = null;
                _typeface = SKTypeface.FromFile(_config.FontFile);
                _linePaint = new SKPaint()
                {
                    Color = Colors.White,
                    Style = SKPaintStyle.Stroke,
                    StrokeCap = SKStrokeCap.Square,
                    StrokeJoin = SKStrokeJoin.Bevel,
                    StrokeWidth = (float)(2 * DisplayScale),
                    IsAntialias = AntiAlias
                };
                _rectPaint = new SKPaint()
                {
                    Color = Colors.White,
                    Style = SKPaintStyle.StrokeAndFill,
                    StrokeCap = SKStrokeCap.Square,
                    StrokeJoin = SKStrokeJoin.Bevel,
                    IsAntialias = AntiAlias
                };
                _textPaint = _textPaint ?? new SKPaint()
                {
                    Color = Colors.White,
                    Typeface = _typeface,
                    TextSize = (float)(12 * DisplayScale),
                    IsStroke = false,
                    IsAntialias = AntiAlias
                };
            }
        }

        public static void Dispose()
        {
            _typeface?.Dispose();
            _linePaint?.Dispose();
            _rectPaint?.Dispose();
            _textPaint?.Dispose();
        }
    }
}

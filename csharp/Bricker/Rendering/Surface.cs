using Bricker.Configuration;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Bricker.Rendering
{
    /// <summary>
    /// Encapsulates the SKBitmap and SKCanvas classes, providing singular ease of use,
    /// plus built-in support for different display scaling.
    /// </summary>
    public sealed class Surface : IDisposable
    {
        //private
        private readonly double _width;
        private readonly double _height;
        private readonly SKBitmap _bitmap;
        private readonly SKCanvas _canvas;
        private static SKPaint _linePaint;
        private static SKPaint _rectPaint;
        private static SKPaint _textPaint;
        
        //public
        public double Width => _width;
        public double Height => _height;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Surface(double width, double height, SKColor? color = null)
        {
            _width = width;
            _height = height;
            _bitmap = new SKBitmap((int)Math.Round(_width * Config.DisplayScale), (int)Math.Round(_height * Config.DisplayScale));
            _canvas = new SKCanvas(_bitmap);
            _canvas.Clear(color ?? (!Config.Debug ? Colors.Transparent : Colors.DebugBlack1));
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Surface(SKCanvas canvas, double width, double height)
        {
            _width = width;
            _height = height;
            _canvas = canvas;
            _bitmap = null;
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            _bitmap?.Dispose();
            _canvas?.Dispose();
        }


        /// <summary>
        /// Dispose static resources.
        /// </summary>
        public static void DisposeStatic()
        {
            _linePaint?.Dispose();
            _rectPaint?.Dispose();
            _textPaint?.Dispose();
        }

        /// <summary>
        /// Clears the canvas.
        /// </summary>
        public void Clear(SKColor? color = null)
        {
            _canvas.Clear(color ?? (!Config.Debug ? Colors.Transparent : Colors.DebugBlack1));
        }

        /// <summary>
        /// Draws a line on the surface.
        /// </summary>
        public void DrawLine(SKColor color, double x0, double y0, double x1, double y1, double width = 2d)
        {
            _linePaint = _linePaint ?? new SKPaint()
            {
                Color = Colors.White,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Square,
                StrokeJoin = SKStrokeJoin.Bevel,
                StrokeWidth = (float)(2 * Config.DisplayScale),
                IsAntialias = Config.AntiAlias
            };
            _linePaint.Color = color;
            _linePaint.StrokeWidth = (float)(width * Config.DisplayScale);
            _canvas.DrawLine((float)(x0 * Config.DisplayScale), (float)(y0 * Config.DisplayScale), (float)(x1 * Config.DisplayScale), (float)(y1 * Config.DisplayScale), _linePaint);
        }

        /// <summary>
        /// Draws a rectangle on the surface.
        /// </summary>
        public void DrawRect(SKColor color, double x, double y, double width, double height)
        {
            _rectPaint = _rectPaint ?? new SKPaint()
            {
                Color = Colors.White,
                Style = SKPaintStyle.StrokeAndFill,
                StrokeCap = SKStrokeCap.Square,
                StrokeJoin = SKStrokeJoin.Bevel,
                IsAntialias = Config.AntiAlias
            };            
            _rectPaint.Color = color;
            _canvas.DrawRect(SKRect.Create((float)(x * Config.DisplayScale), (float)(y * Config.DisplayScale), (float)(width * Config.DisplayScale), (float)(height * Config.DisplayScale)), _rectPaint);
        }

        /// <summary>
        /// Calculates height and width of text.
        /// </summary>
        public static void MeasureText(string text, double size, out double width, out double height)
        {
            _textPaint = _textPaint ?? new SKPaint()
            {
                Color = Colors.White,
                Typeface = Config.Typeface,
                TextSize = (float)(12 * Config.DisplayScale),
                IsStroke = false,
                IsAntialias = Config.AntiAlias
            };

            _textPaint.TextSize = (float)(size * Config.DisplayScale);
            SKRect r = new SKRect();
            _textPaint.MeasureText(text, ref r);

            width = (r.Width / Config.DisplayScale) + 6;
            height = size * 1.333;
        }

        /// <summary>
        /// Creates and returns a surface with text, which must be disposed.
        /// </summary>
        public static Surface RenderText(SKColor color, string text, double size)
        {
            MeasureText(text, size, out double width, out double height);
            _textPaint.Color = color;

            float x = (float)(2 * Config.DisplayScale);
            float y = (float)((size + (size * 0.05)) * Config.DisplayScale);

            Surface surface = new Surface(width, height);
            surface._canvas.Clear(!Config.Debug ? Colors.Transparent : Colors.DebugBlack2);
            surface._canvas.DrawText(text, x, y, _textPaint);
            return surface;
        }

        /// <summary>
        /// Creates and returns a surface with text, which must be disposed.
        /// </summary>
        public static Surface RenderText(SKColor color, IList<string> lines, double size, double spacing = 0)
        {
            double lineWidth = 0, lineHeight = 0;
            foreach (string line in lines)
            {
                MeasureText(line, size, out double w, out double h);
                if (w > lineWidth)
                    lineWidth = w;
                if (h > lineHeight)
                    lineHeight = h;
            }

            double width = lineWidth;
            double height = (lineHeight * lines.Count) + (spacing * (lines.Count - 1));

            Surface surface = new Surface(width, height);
            for (int i = 0; i < lines.Count; i++)
            {
                using (Surface line = RenderText(color, lines[i], size))
                {
                    surface.Blit(line, 0, i * (lineHeight + spacing));
                }
            }
            return surface;
        }

        /// <summary>
        /// Draws text on this surface, horizontally (x) centered, at specified y.
        /// </summary>
        public void DrawText_Centered(SKColor color, string text, double size, double y)
        {
            using (Surface surface = RenderText(color, text, size))
            {
                Blit(surface, (_width - surface.Width) / 2, y);
            }
        }

        /// <summary>
        /// Draws text on this surface, left aligned, at specified y.
        /// </summary>
        public void DrawText_Left(SKColor color, string text, double size, double y, double xSpacing = 0)
        {
            using (Surface surface = RenderText(color, text, size))
            {
                Blit(surface, xSpacing, y);
            }
        }

        /// <summary>
        /// Draws text on this surface, right aligned, at specified y.
        /// </summary>
        public void DrawText_Right(SKColor color, string text, double size, double y, double xSpacing = 0)
        {
            using (Surface surface = RenderText(color, text, size))
            {
                Blit(surface, (_width - surface.Width) - xSpacing, y);
            }
        }

        /// <summary>
        /// Blits one surface onto another.
        /// </summary>
        public void Blit(Surface surface, double x, double y)
        {
            _canvas.DrawBitmap(surface._bitmap, (float)(x * Config.DisplayScale), (float)(y * Config.DisplayScale));
        }



    }
}

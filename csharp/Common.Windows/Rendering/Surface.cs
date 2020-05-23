using Common.Rendering;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Common.Windows.Rendering
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
            _bitmap = new SKBitmap((int)Math.Round(_width * RenderProps.DisplayScale), (int)Math.Round(_height * RenderProps.DisplayScale));
            _canvas = new SKCanvas(_bitmap);
            _canvas.Clear(color ?? (!RenderProps.Debug ? Colors.Transparent : Colors.DebugBlack1));
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Surface(SKCanvas canvas, double width, double height, SKColor? color = null)
        {
            _width = width;
            _height = height;
            _canvas = canvas;
            _bitmap = null;
            _canvas.Clear(color ?? Colors.Transparent);
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
        /// Clears the canvas.
        /// </summary>
        public void Clear(SKColor? color = null)
        {
            _canvas.Clear(color ?? (!RenderProps.Debug ? Colors.Transparent : Colors.DebugBlack1));
        }

        /// <summary>
        /// Draws a line on the surface.
        /// </summary>
        public void DrawLine(SKColor color, double x0, double y0, double x1, double y1, double width = 2d)
        {
            RenderProps.LinePaint.Color = color;
            RenderProps.LinePaint.StrokeWidth = (float)(width * RenderProps.DisplayScale);
            _canvas.DrawLine((float)(x0 * RenderProps.DisplayScale), (float)(y0 * RenderProps.DisplayScale), (float)(x1 * RenderProps.DisplayScale), (float)(y1 * RenderProps.DisplayScale), RenderProps.LinePaint);
        }

        /// <summary>
        /// Draws a rectangle on the surface.
        /// </summary>
        public void DrawRect(SKColor color, double x, double y, double width, double height)
        {
            RenderProps.RectPaint.Color = color;
            _canvas.DrawRect(SKRect.Create((float)(x * RenderProps.DisplayScale), (float)(y * RenderProps.DisplayScale), (float)(width * RenderProps.DisplayScale), (float)(height * RenderProps.DisplayScale)), RenderProps.RectPaint);
        }

        /// <summary>
        /// Calculates height and width of text.
        /// </summary>
        public static void MeasureText(string text, double size, out double width, out double height)
        {
            RenderProps.TextPaint.TextSize = (float)(size * RenderProps.DisplayScale);
            SKRect r = new SKRect();
            RenderProps.TextPaint.MeasureText(text, ref r);

            width = (r.Width / RenderProps.DisplayScale) + 6;
            height = size * 1.333;
        }

        /// <summary>
        /// Calculates width of text.
        /// </summary>
        public static double MeasureText_Width(string text, double size)
        {
            MeasureText(text, size, out double width, out double _);
            return width;
        }

        /// <summary>
        /// Calculates height of text.
        /// </summary>
        public static double MeasureText_Height(string text, double size)
        {
            MeasureText(text, size, out double _, out double height);
            return height;
        }

        /// <summary>
        /// Creates and returns a surface with text, which must be disposed.
        /// </summary>
        public static Surface RenderText(SKColor color, string text, double size)
        {
            MeasureText(text, size, out double width, out double height);
            RenderProps.TextPaint.Color = color;

            float x = (float)(2 * RenderProps.DisplayScale);
            float y = (float)((size + (size * 0.05)) * RenderProps.DisplayScale);

            Surface surface = new Surface(width, height);
            surface._canvas.Clear(!RenderProps.Debug ? Colors.Transparent : Colors.DebugBlack2);
            surface._canvas.DrawText(text, x, y, RenderProps.TextPaint);
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
            double offset = size > 18 ? size / 12d : 0;
            if (offset > 0)
            {
                using (Surface surface = RenderText(Colors.GetMuchDarker(color), text, size))
                {
                    Blit(surface, (_width - surface.Width) / 2 - (offset * 1.2), y + (offset * 0.8));
                }
            }
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
            double offset = size > 18 ? size / 12d : 0;
            if (offset > 0)
            {
                using (Surface surface = RenderText(Colors.GetMuchDarker(color), text, size))
                {
                    Blit(surface, xSpacing - (offset * 1.2), y + (offset * 0.8));
                }
            }
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
            double offset = size > 18 ? size / 12d : 0;
            if (offset > 0)
            {
                using (Surface surface = RenderText(Colors.GetMuchDarker(color), text, size))
                {
                    Blit(surface, (_width - surface.Width) - xSpacing - (offset * 1.2), y + (offset * 0.8));
                }
            }
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
            _canvas.DrawBitmap(surface._bitmap, (float)(x * RenderProps.DisplayScale), (float)(y * RenderProps.DisplayScale));
        }

        /// <summary>
        /// Blits an image onto a surface.
        /// </summary>
        public void Blit(Image image, double x, double y)
        {
            _canvas.DrawImage(image.Bitmap, (float)(x * RenderProps.DisplayScale), (float)(y * RenderProps.DisplayScale));
        }

        /// <summary>
        /// Measures and splits the text into appropriate length lines for display in a message box.
        /// </summary>
        public static string[] WrapText(string message, double size, double width)
        {
            string[] lines = message.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            List<string> text = new List<string>();
            foreach (string line in lines)
            {
                string[] split = line.Split(' ');
                List<string> words = new List<string>();
                foreach (string word in split)
                {
                    string test = String.Join(" ", words);
                    test += (test.Length > 0 ? " " : "") + word;
                    Surface.MeasureText(test, size, out double w, out double _);
                    if (w > width)
                    {
                        text.Add(String.Join(" ", words));
                        words.Clear();
                    }
                    words.Add(word);
                }
                text.Add(String.Join(" ", words));
            }
            return text.ToArray();
        }

    }
}

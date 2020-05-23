using Common.Windows.Rendering;
using SkiaSharp;
using System;
using System.IO;

namespace Common.Rendering
{
    /// <summary>
    /// Stores an image for rendering using our Skia graphics engine.
    /// </summary>
    public class Image : IDisposable
    {
        //public
        public string FilePath { get; }
        public double Width { get; }
        public double Height { get; }
        public SKImage Bitmap { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Image(string filePath)
        {
            FilePath = filePath;
            using (FileStream fs = File.OpenRead(filePath))
            using (SKManagedStream ms = new SKManagedStream(fs))
            using (SKBitmap bmp = SKBitmap.Decode(ms))
                Bitmap = SKImage.FromBitmap(bmp);
            Width = Bitmap.Width;
            Height = Bitmap.Height;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Image(string filePath, SKImage image)
        {
            FilePath = filePath;
            Bitmap = image;
            Width = Bitmap.Width / RenderProps.DisplayScale;
            Height = Bitmap.Height / RenderProps.DisplayScale;
        }

        /// <summary>
        /// Returns a new resized image.
        /// </summary>
        public Image Resize(double width, double height)
        {
            using (SKSurface surface = SKSurface.Create(new SKImageInfo((int)(width * RenderProps.DisplayScale), (int)(height * RenderProps.DisplayScale), SKImageInfo.PlatformColorType, SKAlphaType.Premul)))
            using (SKPaint paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.FilterQuality = SKFilterQuality.High;
                surface.Canvas.DrawImage(Bitmap, new SKRectI(0, 0, (int)(width * RenderProps.DisplayScale), (int)(height * RenderProps.DisplayScale)), paint);
                surface.Canvas.Flush();
                return new Image(FilePath, surface.Snapshot());
            }
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            Bitmap?.Dispose();
        }
    }
}

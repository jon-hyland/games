using SkiaSharp;
using System;

namespace Bricker.Rendering.Tiles
{
    public interface ITile
    {
        double X { get; }
        double Y { get; }
        double Width { get; }
        double Height { get; }
        SKColor Color { get; }

        void Move(DateTime now, double level);
    }
}

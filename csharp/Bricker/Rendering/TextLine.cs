using Common.Rendering;
using SkiaSharp;

namespace Bricker.Rendering
{
    /// <summary>
    /// Represets a single line of text in a message box or popup.
    /// </summary>
    public class TextLine
    {
        public string Text { get; set; }
        public double Size { get; set; }
        public SKColor Color { get; set; }
        public double TopMargin { get; set; }
        public double BottomMargin { get; set; }
        public TextAlignment Alignment { get; set; }

        public TextLine(string text = "", double size = 18, SKColor? color = null, double topMargin = 0, double bottomMargin = 0, TextAlignment alignment = TextAlignment.Left)
        {
            Text = text;
            Size = size;
            Color = color != null ? (SKColor)color : Colors.White;
            TopMargin = topMargin;
            BottomMargin = bottomMargin;
            Alignment = alignment;
        }
    }

    /// <summary>
    /// Represents alignment of text.
    /// </summary>
    public enum TextAlignment
    {
        Left,
        Right,
        Center
    }
}

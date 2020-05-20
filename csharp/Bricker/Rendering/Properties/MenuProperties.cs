using System;
using System.Linq;

namespace Bricker.Rendering.Properties
{
    public class MenuProperties
    {
        public string[] Items { get; }
        public double FontSize { get; }
        public bool[] EnabledItems { get; }
        public string[] Header { get; }
        public double HeaderSize { get; }
        public bool AllowEsc { get; }
        public bool AllowPlayerInvite { get; }
        public int SelectedIndex { get; private set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double CalculatedHeight { get; set; }
        public double HeaderLineHeight { get; set; }
        public double ItemLineHeight { get; set; }

        public MenuProperties(string[] items, double fontSize = 42, bool[] enabledItems = null, string[] header = null, double headerSize = 24, bool allowEsc = false, bool allowPlayerInvite = false, int selectedIndex = 0, double width = Double.NaN, double height = Double.NaN)
        {
            Items = items;
            FontSize = fontSize;
            EnabledItems = enabledItems ?? Enumerable.Repeat(true, items.Length).ToArray();
            if (EnabledItems.Length != items.Length)
                throw new Exception("Array lengths do not match");
            if (!EnabledItems.Where(o => o == true).Any())
                throw new Exception("All options cannot be disabled");
            Header = header ?? new string[0];
            HeaderSize = headerSize;
            AllowEsc = allowEsc;
            AllowPlayerInvite = allowPlayerInvite;
            SelectedIndex = selectedIndex;
            Width = width;
            Height = height;
            CalculatedHeight = Double.NaN;
            HeaderLineHeight = Double.NaN;
            ItemLineHeight = Double.NaN;
            while (!EnabledItems[SelectedIndex])
                IncrementSelection();
        }

        public int IncrementSelection()
        {
            SelectedIndex++;
            if (SelectedIndex >= Items.Length)
                SelectedIndex = 0;
            if (!EnabledItems[SelectedIndex])
                return IncrementSelection();
            return SelectedIndex;
        }

        public int DecrementSelection()
        {
            SelectedIndex--;
            if (SelectedIndex < 0)
                SelectedIndex = Items.Length - 1;
            if (!EnabledItems[SelectedIndex])
                return DecrementSelection();
            return SelectedIndex;
        }
    }

}

using System;

namespace Bricker.Rendering.Properties
{
    public class SettingsProperties
    {
        public SettingsItem[] Items { get; }
        public double FontSize { get; }
        public int SelectedIndex { get; private set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double CalculatedHeight { get; set; }
        public double ItemLineHeight { get; set; }

        public SettingsProperties(SettingsItem[] items, double fontSize = 42, int selectedIndex = 0, double width = Double.NaN, double height = Double.NaN)
        {
            Items = items;
            FontSize = fontSize;
            SelectedIndex = selectedIndex;
            Width = width;
            Height = height;
            CalculatedHeight = Double.NaN;
        }

        public int IncrementSelection()
        {
            SelectedIndex++;
            if (SelectedIndex >= Items.Length)
                SelectedIndex = 0;
            return SelectedIndex;
        }

        public int DecrementSelection()
        {
            SelectedIndex--;
            if (SelectedIndex < 0)
                SelectedIndex = Items.Length - 1;
            return SelectedIndex;
        }
    }

    public class SettingsItem
    {
        public string OnCaption { get; }
        public string OffCaption { get; }
        public bool Value { get; set; }
        public string Caption => Value ? OnCaption : OffCaption;

        public SettingsItem(string onCaption, string offCaption, bool value)
        {
            OnCaption = onCaption;
            OffCaption = offCaption;
            Value = value;
        }
    }
}

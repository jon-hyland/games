using System;
using System.Linq;

namespace Bricker.Rendering.Properties
{
    public class MenuProperties
    {
        public string[] Options { get; }
        public double OptionsSize { get; }
        public bool[] EnabledOptions { get; }
        public string[] Header { get; }
        public double HeaderSize { get; }
        public bool AllowEsc { get; }
        public bool AllowPlayerInvite { get; }
        public int SelectionIndex { get; private set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double HeaderLineHeight { get; set; }
        public double OptionLineHeight { get; set; }

        public MenuProperties(string[] options, double optionsSize = 42, bool[] enabledOptions = null, string[] header = null, double headerSize = 24, bool allowEsc = false, bool allowPlayerInvite = false, int selectionIndex = 0, double width = Double.NaN)
        {
            Options = options;
            OptionsSize = optionsSize;
            EnabledOptions = enabledOptions ?? Enumerable.Repeat(true, options.Length).ToArray();
            if (EnabledOptions.Length != options.Length)
                throw new Exception("Array lengths do not match");
            if (!EnabledOptions.Where(o => o == true).Any())
                throw new Exception("All options cannot be disabled");
            Header = header ?? new string[0];
            HeaderSize = headerSize;
            AllowEsc = allowEsc;
            AllowPlayerInvite = allowPlayerInvite;
            SelectionIndex = selectionIndex;
            Width = width;
            Height = Double.NaN;
            HeaderLineHeight = Double.NaN;
            OptionLineHeight = Double.NaN;
            while (!EnabledOptions[SelectionIndex])
                IncrementSelection();
        }

        public int IncrementSelection()
        {
            SelectionIndex++;
            if (SelectionIndex >= Options.Length)
                SelectionIndex = 0;
            if (!EnabledOptions[SelectionIndex])
                return IncrementSelection();
            return SelectionIndex;
        }

        public int DecrementSelection()
        {
            SelectionIndex--;
            if (SelectionIndex < 0)
                SelectionIndex = Options.Length - 1;
            if (!EnabledOptions[SelectionIndex])
                return DecrementSelection();
            return SelectionIndex;
        }


    }
}

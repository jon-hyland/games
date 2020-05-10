using Common.Windows.Configuration;
using SkiaSharp;

namespace Common.Windows.Rendering
{
    public static class RenderProps
    {
        public static SKTypeface Typeface { get; private set; }
        public static bool AntiAlias { get; private set; }
        public static bool HighFrameRate { get; private set; }
        public static bool Debug { get; set; }
        public static double DisplayScale { get; set; }

        public static void Initialize(IConfig config)
        {
            Typeface = config.Typeface;
            AntiAlias = config.AntiAlias;
            HighFrameRate = config.HighFrameRate;
            Debug = config.Debug;
            DisplayScale = 1d;
        }

    }
}

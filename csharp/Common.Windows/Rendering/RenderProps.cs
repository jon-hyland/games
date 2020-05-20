using Common.Standard.Configuration;
using SkiaSharp;

namespace Common.Windows.Rendering
{
    public static class RenderProps
    {
        public static SKTypeface Typeface { get; private set; }
        public static bool AntiAlias { get; private set; }
        public static bool HighFrameRate { get; private set; }
        public static bool Background { get; set; }
        public static bool Debug { get; set; }
        public static double DisplayScale { get; set; }

        public static void Initialize(IGameConfig config)
        {
            Typeface = SKTypeface.FromFile(config.FontFile);
            AntiAlias = config.AntiAlias;
            HighFrameRate = config.HighFrameRate;
            Background = config.Background;
            Debug = config.Debug;
            DisplayScale = 1d;
        }

    }
}

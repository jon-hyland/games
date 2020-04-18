using Bricker.Utilities;
using System;
using System.IO;
using System.Reflection;

namespace Bricker.Configuration
{
    /// <summary>
    /// Loads and exposes configuration settings and properties.
    /// </summary>
    public static class Config
    {
        public static string Version { get; }
        public static string ApplicationFolder { get; }
        public static string ConfigFile { get; }
        public static string FontFile { get; }
        public static string HighScoreFile { get; }
        public static bool AntiAlias { get; }
        public static bool Debug { get; set; }

        static Config()
        {
            Version = GetVersion();
            ApplicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ConfigFile = Path.Combine(ApplicationFolder, "Config.json");
            FontFile = Path.Combine(ApplicationFolder, "Zorque.ttf");
            HighScoreFile = Path.Combine(ApplicationFolder, "HighScores.txt");

            dynamic data = JsonSerialization.Deserialize(File.ReadAllText(ConfigFile));
            AntiAlias = data.antiAlias == 1;
            Debug = data.debug == 1;
        }

        private static string GetVersion()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }
}

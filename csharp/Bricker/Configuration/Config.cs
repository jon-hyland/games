using Bricker.Error;
using Bricker.Networking;
using Common.Json;
using SkiaSharp;
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
        public static string InitialsFile { get; }
        public static string RemoteInstanceFile { get; }
        public static SKTypeface Typeface { get; }
        public static bool AntiAlias { get; }
        public static bool HighFrameRate { get; }
        public static bool Debug { get; set; }
        public static double DisplayScale { get; private set; }
        public static string Initials { get; private set; }
        public static string LocalIP { get; }

        static Config()
        {
            Version = GetVersion();
            ApplicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ConfigFile = Path.Combine(ApplicationFolder, "Config.json");
            FontFile = Path.Combine(ApplicationFolder, "Zorque.ttf");
            HighScoreFile = Path.Combine(ApplicationFolder, "HighScores.txt");
            InitialsFile = Path.Combine(ApplicationFolder, "Initials.txt");
            RemoteInstanceFile = Path.Combine(ApplicationFolder, "RemoteInstances.txt");
            Typeface = SKTypeface.FromFile(FontFile);

            dynamic data = JsonSerialization.Deserialize(File.ReadAllText(ConfigFile));
            AntiAlias = data.antiAlias == 1;
            HighFrameRate = data.highFrameRate == 1;
            Debug = data.debug == 1;
            DisplayScale = 1;
            Initials = LoadInitials();
            LocalIP = NetworkDiscovery.GetLocalIP();
        }

        private static string GetVersion()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
        
        public static void SetDisplayScale(double scale)
        {
            DisplayScale = scale;
        }

        public static void SaveInitials(string initials)
        {
            try
            {
                Initials = CleanInitials(initials);
                File.WriteAllText(Config.InitialsFile, (Initials ?? "").Trim());
            }
            catch
            {
            }
        }

        private static string LoadInitials()
        {
            try
            {
                if (File.Exists(InitialsFile))
                {
                    return CleanInitials(File.ReadAllText(InitialsFile));
                }
            }
            catch
            {
            }
            return "";
        }

        public static string CleanInitials(string initials)
        {
            initials = initials.ToUpper().Trim();
            if (initials.Length > 3)
                initials = initials.Substring(0, 3);
            return initials;
        }
    }
}

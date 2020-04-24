using Common.Json;
using Common.Networking;
using SkiaSharp;
using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace Bricker.Configuration
{
    /// <summary>
    /// Loads and exposes configuration settings and properties.
    /// </summary>
    public static class Config
    {
        public static string GameTitle { get; }
        public static Version GameVersion { get; }
        public static string DisplayVersion { get; }
        public static IPAddress LocalIP { get; }
        public static ushort LocalPort { get; }
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

        static Config()
        {
            GameTitle = "Bricker";
            GameVersion = GetVersion();
            DisplayVersion = $"{GameVersion.Major}.{GameVersion.Minor}.{GameVersion.Build}";
            LocalIP = NetworkDiscovery.GetLocalIP();
            LocalPort = 8714;
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
        }

        private static Version GetVersion()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;            
            return version;
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

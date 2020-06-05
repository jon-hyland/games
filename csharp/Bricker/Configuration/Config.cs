using Common.Standard.Configuration;
using Common.Standard.Json;
using Common.Standard.Networking;
using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace Bricker.Configuration
{
    /// <summary>
    /// Loads and exposes configuration settings and properties.
    /// </summary>
    public class Config : IGameConfig
    {
        public string GameTitle { get; }
        public Version GameVersion { get; }
        public string DisplayVersion { get; }
        public IPAddress LocalIP { get; }
        public IPAddress ServerIP { get; }
        public ushort ServerPort { get; }

        public string ApplicationFolder { get; }
        public string AudioSampleFolder { get; }
        public string ImageFolder { get; }
        public string ConfigFile { get; }
        public string LogFile { get; }
        public string FontFile { get; }
        public string HighScoreFile { get; }
        public string InitialsFile { get; }
        public string RemoteInstanceFile { get; }

        public bool Music { get; set; }
        public bool SoundEffects { get; set; }
        public bool ShowGhost { get; set; }
        public bool AntiAlias { get; set; }
        public bool HighFrameRate { get; set; }
        public bool ShowBackground { get; set; }
        public bool Debug { get; set; }
        public string Initials { get; private set; }

        public Config()
        {
            ApplicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ConfigFile = Path.Combine(ApplicationFolder, "Config.json");
            dynamic data = JsonParse.Deserialize(File.ReadAllText(ConfigFile));
            GameTitle = "Bricker";
            GameVersion = GetVersion();
            DisplayVersion = $"{GameVersion.Major}.{GameVersion.Minor}.{GameVersion.Build}";
            LocalIP = InterfaceDiscovery.GetLocalIP();
            ServerIP = JsonParse.GetIPAddress(data.multiplayer.server) ?? DnsHelper.ResolveHost(JsonParse.GetString(data.multiplayer.server));
            ServerPort = JsonParse.GetUShort(data.multiplayer.port);

            AudioSampleFolder = Path.Combine(ApplicationFolder, "Samples");
            ImageFolder = Path.Combine(ApplicationFolder, "Images");
            LogFile = Path.Combine(ApplicationFolder, "LogFile.txt");
            FontFile = Path.Combine(ApplicationFolder, "Zorque.ttf");
            HighScoreFile = Path.Combine(ApplicationFolder, "HighScores.txt");
            InitialsFile = Path.Combine(ApplicationFolder, "Initials.txt");
            RemoteInstanceFile = Path.Combine(ApplicationFolder, "RemoteInstances.txt");

            Music = JsonParse.GetBoolean(data.audio.music);
            SoundEffects = JsonParse.GetBoolean(data.audio.effects);
            ShowGhost = JsonParse.GetBoolean(data.display.ghost);
            ShowBackground = JsonParse.GetBoolean(data.display.background);
            HighFrameRate = JsonParse.GetBoolean(data.performance.highFrameRate);
            AntiAlias = JsonParse.GetBoolean(data.performance.antiAlias);
            ImageFolder = JsonParse.GetString(data.display.imageFolder, null) ?? ImageFolder;
            if (ImageFolder.StartsWith("./"))
                ImageFolder = Path.Combine(ApplicationFolder, ImageFolder.Substring(2));
            Debug = JsonParse.GetBoolean(data.debug);

            Initials = LoadInitials();
        }

        private static Version GetVersion()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            return version;
        }

        public void SaveInitials(string initials)
        {
            try
            {
                Initials = CleanInitials(initials);
                File.WriteAllText(InitialsFile, (Initials ?? "").Trim());
            }
            catch
            {
            }
        }

        private string LoadInitials()
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

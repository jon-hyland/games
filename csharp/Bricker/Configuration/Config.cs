﻿using Common.Standard.Json;
using Common.Standard.Networking;
using Common.Windows.Configuration;
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
    public class Config : IConfig
    {
        public string GameTitle { get; }
        public Version GameVersion { get; }
        public string DisplayVersion { get; }
        public IPAddress LocalIP { get; }
        public ushort GamePort { get; }
        public string ApplicationFolder { get; }
        public string ConfigFile { get; }
        public string LogFile { get; }
        public string FontFile { get; }
        public string HighScoreFile { get; }
        public string InitialsFile { get; }
        public string RemoteInstanceFile { get; }
        public SKTypeface Typeface { get; }
        public bool AntiAlias { get; }
        public bool HighFrameRate { get; }
        public bool Debug { get; }
        public string Initials { get; private set; }

        public Config()
        {
            GameTitle = "Bricker";
            GameVersion = GetVersion();
            DisplayVersion = $"{GameVersion.Major}.{GameVersion.Minor}.{GameVersion.Build}";
            LocalIP = InterfaceDiscovery.GetLocalIP();
            GamePort = 8714;
            ApplicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ConfigFile = Path.Combine(ApplicationFolder, "Config.json");
            LogFile = Path.Combine(ApplicationFolder, "LogFile.txt");
            FontFile = Path.Combine(ApplicationFolder, "Zorque.ttf");
            HighScoreFile = Path.Combine(ApplicationFolder, "HighScores.txt");
            InitialsFile = Path.Combine(ApplicationFolder, "Initials.txt");
            RemoteInstanceFile = Path.Combine(ApplicationFolder, "RemoteInstances.txt");
            Typeface = SKTypeface.FromFile(FontFile);

            dynamic data = JsonSerialization.Deserialize(File.ReadAllText(ConfigFile));
            AntiAlias = data.antiAlias == 1;
            HighFrameRate = data.highFrameRate == 1;
            Debug = data.debug == 1;
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

using Common.Standard.Networking;
using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace GameServer.Configuration
{
    /// <summary>
    /// Loads and exposes configuration settings and properties.
    /// </summary>
    public class Config
    {
        public Version ServiceVersion { get; }
        public string DisplayVersion { get; }
        public IPAddress LocalIP { get; }
        public ushort ListenPort { get; }
        public string ApplicationFolder { get; }
        public string ConfigFile { get; }
        public string LogFile { get; }

        public Config()
        {
            ServiceVersion = GetVersion();
            DisplayVersion = $"{ServiceVersion.Major}.{ServiceVersion.Minor}.{ServiceVersion.Build}";
            LocalIP = InterfaceDiscovery.GetLocalIP();
            ListenPort = 8780;
            ApplicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ConfigFile = Path.Combine(ApplicationFolder, "Config.json");
            LogFile = Path.Combine(ApplicationFolder, "LogFile.txt");

            //dynamic data = JsonSerialization.Deserialize(File.ReadAllText(ConfigFile));
        }

        private static Version GetVersion()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            return version;
        }
    }
}

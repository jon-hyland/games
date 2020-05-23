using System;
using System.Net;

namespace Common.Standard.Configuration
{
    public interface IGameConfig
    {
        string GameTitle { get; }
        Version GameVersion { get; }
        string DisplayVersion { get; }
        IPAddress LocalIP { get; }
        IPAddress ServerIP { get; }
        ushort ServerPort { get; }

        string ApplicationFolder { get; }
        string AudioSampleFolder { get; }
        string ConfigFile { get; }
        string FontFile { get; }

        bool Music { get; set; }
        bool SoundEffects { get; set; }
        bool Ghost { get; set; }
        bool Background { get; set; }
        bool HighFrameRate { get; set; }
        bool AntiAlias { get; set; }
        bool Debug { get; set; }
    }
}

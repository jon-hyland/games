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
        string ApplicationFolder { get; }
        string AudioSampleFolder { get; }
        string ConfigFile { get; }
        string FontFile { get; }
        bool AntiAlias { get; }
        bool HighFrameRate { get; }
        bool Background { get; }
        bool Debug { get; }
        bool Music { get; }
        bool SoundEffects { get; }
        IPAddress ServerIP { get; }
        ushort ServerPort { get; }
    }
}

using SkiaSharp;
using System;
using System.Net;

namespace Common.Windows.Configuration
{
    public interface IConfig
    {
        string GameTitle { get; }
        Version GameVersion { get; }
        string DisplayVersion { get; }
        IPAddress LocalIP { get; }
        ushort GamePort { get; }
        string ApplicationFolder { get; }
        string ConfigFile { get; }
        string FontFile { get; }
        SKTypeface Typeface { get; }
        bool AntiAlias { get; }
        bool HighFrameRate { get; }
        bool Debug { get; }
    }
}

using System;

namespace Common.Logging
{
    public static class Log
    {
        private static ILogger _logger = null;
        private static LogLevel _defaultLevel = LogLevel.Medium;
        private static string _defaultHeader = "Unspecified";

        public static void Initiallize(ILogger logger, LogLevel defaultLevel = LogLevel.Medium, string defaultHeader = "Unspecified")
        {
            _logger = logger;
            _defaultHeader = defaultHeader;
            _defaultLevel = defaultLevel;
        }

        public static void Write(string message)
        {
            _logger?.Write(_defaultLevel, _defaultHeader, message);
        }

        public static void Write(string header, string message)
        {
            _logger?.Write(_defaultLevel, header, message);
        }

        public static void Write(LogLevel level, string header, string message)
        {
            _logger?.Write(level, header, message);
        }

        public static void Error(Exception ex)
        {
            _logger?.Error(ex);
        }
    }
}

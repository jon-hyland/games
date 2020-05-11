using System;

namespace Common.Standard.Logging
{
    public static class Log
    {
        private static ILogger _logger = null;

        public static void Initiallize(ILogger logger)
        {
            _logger = logger;
        }

        public static void Write(string message)
        {
            _logger?.Write(message);
        }

        public static void Error(Exception ex)
        {
            _logger?.Error(ex);
        }
    }
}

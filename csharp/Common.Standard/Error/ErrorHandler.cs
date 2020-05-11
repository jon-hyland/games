using Common.Standard.Logging;
using System;

namespace Common.Standard.Error
{
    public static class ErrorHandler
    {
        private static ILogger _logger = null;

        public static void Initialize(ILogger logger)
        {
            _logger = logger;
        }

        public static void LogError(Exception ex)
        {
            _logger?.Error(ex);
        }
    }
}

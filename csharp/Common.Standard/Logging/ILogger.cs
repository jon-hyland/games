using System;

namespace Common.Standard.Logging
{
    /// <summary>
    /// Defines common logging abilities.
    /// </summary>
    public interface ILogger
    {
        void Write(LogLevel level, string header, string message);
        void Error(Exception ex);
    }
}

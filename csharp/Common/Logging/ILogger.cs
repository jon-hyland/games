namespace Common.Logging
{
    /// <summary>
    /// Defines common logging abilities.
    /// </summary>
    public interface ILogger
    {
        void Write(LogLevel level, string header, string message);
    }
}

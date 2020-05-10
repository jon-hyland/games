namespace Common.Standard.Networking
{
    /// <summary>
    /// The result of a command request.
    /// </summary>
    public enum CommandResult : byte
    {
        Unspecified = 0,
        Error = 1,
        Timeout = 2,
        Reject = 3,
        Accept = 4
    }
}

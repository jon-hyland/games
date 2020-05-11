namespace Common.Standard.Networking
{
    /// <summary>
    /// The result of a command request.
    /// </summary>
    public class CommandResult
    {
        public ResultCode Code { get; set; }
        public byte[] Data { get; set; }

        public CommandResult(ResultCode code, byte[] data = null)
        {
            Code = code;
            Data = data ?? new byte[0];
        }
    }

    /// <summary>
    /// The result code of a command request.
    /// </summary>
    public enum ResultCode : byte
    {
        Unspecified = 0,
        Error = 1,
        Timeout = 2,
        Reject = 3,
        Accept = 4
    }
}

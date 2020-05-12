using Common.Standard.Networking.Packets;

namespace Common.Standard.Networking
{
    /// <summary>
    /// The result of a command request.
    /// </summary>
    public class CommandResult
    {
        public ResultCode Code { get; set; }
        public CommandResponsePacket ResponsePacket { get; set; }

        public CommandResult(ResultCode code, CommandResponsePacket responsePacket = null)
        {
            Code = code;
            ResponsePacket = responsePacket;
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

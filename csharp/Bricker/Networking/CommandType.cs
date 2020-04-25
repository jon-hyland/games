namespace Bricker.Networking
{
    /// <summary>
    /// Represents the different types of commands one Bricker instance
    /// can send another.  For all games:
    ///   0 = Unspecified
    ///   1 = ConnectRequest
    ///   2 = DisconnectRequest
    /// </summary>
    public enum CommandType : ushort
    {
        Unspecified = 0,
        ConnectRequest = 1,
        DisconnectRequest = 2

    }
}

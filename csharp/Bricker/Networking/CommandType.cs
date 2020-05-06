namespace Bricker.Networking
{
    /// <summary>
    /// Represents the different types of commands one Bricker instance
    /// can send another.  For all games:
    ///   0 = Unspecified
    ///   1 = ConnectRequest
    ///   2 = DisconnectRequest
    ///   3 = GameOver
    /// </summary>
    public enum CommandType : ushort
    {
        Unspecified = 0,
        ConnectRequest = 1,
        DisconnectRequest = 2,
        GameOver = 3

    }
}

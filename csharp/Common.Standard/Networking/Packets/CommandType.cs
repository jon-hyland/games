namespace Common.Standard.Networking.Packets
{
    /// <summary>
    /// Represents the different types of commands one player can send the server,
    /// one of which bounce to the opponent for response.
    /// </summary>
    public enum CommandType : ushort
    {
        Unspecified = 0,
        GetPlayers = 1,
        ConnectToPlayer = 2,
        Disconnect = 3,
        Passthrough = 4
    }
}

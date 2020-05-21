namespace Common.Standard.Networking.Packets
{
    /// <summary>
    /// Represents the different types of commands a player can send the server,
    /// some of which get forwared to the opponent.
    /// </summary>
    public enum CommandType : ushort
    {
        Unspecified = 0,
        GetPlayers = 1,             // sent to server
        ConnectToPlayer = 2,        // passed to opponent, special server logic
        GameOver = 3,               // passed to opponent
        EndSession = 4,             // sent by server
        Passthrough = 5,            // passed to opponent
        QuitGame = 6                // sent to server
    }
}

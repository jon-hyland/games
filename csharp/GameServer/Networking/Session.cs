using Common.Standard.Game;

namespace GameServer.Networking
{
    /// <summary>
    /// Represents a two-player game session where both parties are connected to the
    /// server.  One player initiates a request, the other accepts, and a session is 
    /// created.
    /// </summary>
    public class Session
    {
        public ushort SessionID { get; }
        public Player Player1 { get; }
        public Player Player2 { get; }

        public Session(ushort sessionID, Player player1, Player player2)
        {
            SessionID = sessionID;
            Player1 = player1;
            Player2 = player2;
        }

        public Player GetOpponent(Player player)
        {
            if (player.UniqueKey != Player2.UniqueKey)
                return Player1;
            return Player2;
        }
    }
}

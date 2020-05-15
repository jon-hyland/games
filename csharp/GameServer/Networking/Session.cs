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
        public Player Player1 { get; }
        public Player Player2 { get; }

        public Session(Player player1, Player player2)
        {
            Player1 = player1;
            Player2 = player2;
        }

        public bool ContainsPlayer(Player player)
        {
            if (Player1.UniqueKey == player.UniqueKey)
                return true;
            if (Player2.UniqueKey == player.UniqueKey)
                return true;
            return false;
        }

        public bool ContainsPlayer(int playerKey)
        {
            if (Player1.UniqueKey == playerKey)
                return true;
            if (Player2.UniqueKey == playerKey)
                return true;
            return false;
        }

        public bool ContainsBothPlayers(Player player1, Player player2)
        {
            return ContainsPlayer(player1) && ContainsPlayer(player2);
        }

        public bool ContainsBothPlayers(int player1Key, int player2Key)
        {
            return ContainsPlayer(player1Key) && ContainsPlayer(player2Key);
        }

        public bool ContainsEitherPlayer(Player player1, Player player2)
        {
            return ContainsPlayer(player1) || ContainsPlayer(player2);
        }

        public bool ContainsEitherPlayer(int player1Key, int player2Key)
        {
            return ContainsPlayer(player1Key) || ContainsPlayer(player2Key);
        }

        public Player GetTimedoutPlayer(int timeoutMs)
        {
            Player lastToCheckIn = Player1.TimeSinceLastHeartbeat.TotalMilliseconds >= Player2.TimeSinceLastHeartbeat.TotalMilliseconds ? Player1 : Player2;
            if (lastToCheckIn.TimeSinceLastHeartbeat.TotalMilliseconds > timeoutMs)
                return lastToCheckIn;
            return null;
        }

        public Player GetOpponent(Player player)
        {
            if (player.UniqueKey != Player2.UniqueKey)
                return Player1;
            return Player2;
        }
    }
}

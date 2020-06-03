using Common.Standard.Networking;
using System;

namespace GameServer.Networking
{
    /// <summary>
    /// Represents a two-player game session where both parties are connected to the
    /// server.  One player initiates a request, the other accepts, and a session is 
    /// created.
    /// </summary>
    public class Session
    {
        public NetworkPlayer Player1 { get; }
        public NetworkPlayer Player2 { get; }
        public bool IsConfirmed { get; private set; }
        public DateTime CreateTime { get; set; }
        public TimeSpan TimeSinceCreated => DateTime.Now - CreateTime;

        public Session(NetworkPlayer player1, NetworkPlayer player2)
        {
            Player1 = player1;
            Player2 = player2;
            IsConfirmed = false;
            CreateTime = DateTime.Now;
        }

        public void ConfirmSession()
        {
            IsConfirmed = true;
        }

        public bool ContainsPlayer(NetworkPlayer player)
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

        public bool ContainsBothPlayers(NetworkPlayer player1, NetworkPlayer player2)
        {
            return ContainsPlayer(player1) && ContainsPlayer(player2);
        }

        public bool ContainsBothPlayers(int player1Key, int player2Key)
        {
            return ContainsPlayer(player1Key) && ContainsPlayer(player2Key);
        }

        public bool ContainsEitherPlayer(NetworkPlayer player1, NetworkPlayer player2)
        {
            return ContainsPlayer(player1) || ContainsPlayer(player2);
        }

        public bool ContainsEitherPlayer(int player1Key, int player2Key)
        {
            return ContainsPlayer(player1Key) || ContainsPlayer(player2Key);
        }

        public NetworkPlayer GetTimedoutPlayer(int timeoutMs)
        {
            if (Player1.QuitGame)
                return Player1;
            if (Player2.QuitGame)
                return Player2;
            NetworkPlayer lastToCheckIn = Player1.TimeSinceLastHeartbeat.TotalMilliseconds >= Player2.TimeSinceLastHeartbeat.TotalMilliseconds ? Player1 : Player2;
            if (lastToCheckIn.TimeSinceLastHeartbeat.TotalMilliseconds > timeoutMs)
                return lastToCheckIn;
            return null;
        }

        public NetworkPlayer GetOpponent(NetworkPlayer player)
        {
            if (player.UniqueKey != Player2.UniqueKey)
                return Player1;
            return Player2;
        }
    }
}

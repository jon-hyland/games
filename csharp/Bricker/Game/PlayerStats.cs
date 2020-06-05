namespace Bricker.Game
{
    /// <summary>
    /// Stores basic player statistics.
    /// </summary>
    public class PlayerStats
    {
        //public
        public int Score { get; private set; }
        public int Lines { get; private set; }
        public int Level { get; private set; }
        public int LinesSent { get; private set; }
        public int LastLinesSent { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PlayerStats()
        {
            Score = 0;
            Lines = 0;
            Level = 1;
            LinesSent = 0;
            LastLinesSent = 0;
        }

        /// <summary>
        /// Resets stats for new game.
        /// </summary>
        public void Reset()
        {
            Score = 0;
            Lines = 0;
            Level = 1;
            LinesSent = 0;
            LastLinesSent = 0;
        }

        /// <summary>
        /// Increments current score.
        /// </summary>
        public void IncrementScore(int value)
        {
            Score += value;
        }

        /// <summary>
        /// Increments cleared line count, and sets level.
        /// Returns true on level increase.
        /// </summary>
        public bool IncrementLines(int value)
        {
            Lines += value;
            int newLevel = (Lines / 25) + 1;
            if (newLevel > Lines)
                return SetLevel(newLevel);
            return false;
        }

        /// <summary>
        /// Sets the current level.
        /// </summary>
        public bool SetLevel(int level)
        {
            if (level > 10)
                level = 10;
            if (level < 1)
                level = 1;
            if (level != Level)
            {
                Level = level;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Increments lines sent (two-player mode).
        /// </summary>
        public void IncrementLinesSent(int value)
        {
            LinesSent += value;
        }

        /// <summary>
        /// Sets lines sent (two-player mode).
        /// </summary>
        public void SetLinesSent(int value)
        {
            LinesSent = value;
        }

        /// <summary>
        /// Sets last lines sent.
        /// </summary>
        public void SetLastLinesSent(int value)
        {
            LastLinesSent = value;
        }

    }
}

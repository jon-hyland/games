namespace Bricker.Game
{
    /// <summary>
    /// Stores basic player statistics.
    /// </summary>
    public class PlayerStats
    {
        //public
        public int Level { get; private set; }
        public int Lines { get; private set; }
        public int Score { get; private set; }
        public int LinesSent { get; private set; }
        public int LastLinesSent { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PlayerStats()
        {
            Level = 1;
            Lines = 0;
            Score = 0;
            LinesSent = 0;
            LastLinesSent = 0;
        }

        /// <summary>
        /// Resets stats for new game.
        /// </summary>
        public void Reset()
        {
            Level = 1;
            Lines = 0;
            Score = 0;
            LinesSent = 0;
            LastLinesSent = 0;
        }

        /// <summary>
        /// Sets the current level.
        /// </summary>
        public bool SetLevel(int value)
        {
            if (value > 10)
                value = 10;
            if (value < 1)
                value = 1;
            if (value != Level)
            {
                Level = value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Increments the current level by value.
        /// </summary>
        public bool IncrementLevel(int value)
        {
            int level = Level + value;
            return SetLevel(level);
        }

        /// <summary>
        /// Sets lines to specified value.
        /// </summary>
        public void SetLines(int value)
        {
            Lines += value;
        }

        /// <summary>
        /// Increments cleared line count, and sets level.
        /// Returns true if level increased.
        /// </summary>
        public bool IncrementLinesAndLevel(int value)
        {
            Lines += value;
            int newLevel = (Lines / 25) + 1;
            if (newLevel > Lines)
                return SetLevel(newLevel);
            return false;
        }

        /// <summary>
        /// Sets score to specified value.
        /// </summary>
        public void SetScore(int value)
        {
            Score = value;
        }

        /// <summary>
        /// Increments current score.
        /// </summary>
        public void IncrementScore(int value)
        {
            Score += value;
        }

        /// <summary>
        /// Sets lines sent (two-player mode).
        /// </summary>
        public void SetLinesSent(int value)
        {
            LinesSent = value;
        }

        /// <summary>
        /// Increments lines sent (two-player mode).
        /// </summary>
        public void IncrementLinesSent(int value)
        {
            LinesSent += value;
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

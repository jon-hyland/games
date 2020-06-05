using Bricker.Configuration;
using Common.Standard.Error;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bricker.Game
{
    public class HighScores
    {
        //private
        private readonly Config _config;
        private List<HighScore> _scores;

        //public
        public IReadOnlyList<HighScore> Scores => _scores.AsReadOnly();

        /// <summary>
        /// Class constructor.
        /// </summary>
        public HighScores(Config config)
        {
            _config = config;
            _scores = LoadHighScores();
        }

        /// <summary>
        /// Returns true if score can be placed on board.
        /// </summary>
        public bool IsHighScore(int score)
        {
            if (_scores.Count < 10)
                return true;
            int lowest = Int32.MaxValue;
            foreach (HighScore s in _scores)
                if (s.Score < lowest)
                    lowest = s.Score;
            return score > lowest;
        }

        /// <summary>
        /// Adds a new high score, sorts and keeps top 10, saves to disk.
        /// </summary>
        public void AddHighScore(string initials, int score)
        {
            _scores.Add(new HighScore(initials, score));
            _scores = _scores.OrderByDescending(s => s.Score).Take(10).ToList();
            SaveHighScores();
        }

        /// <summary>
        /// Saves high scores to disk.
        /// </summary>
        private void SaveHighScores()
        {
            try
            {
                string text = String.Join(Environment.NewLine, _scores.Select(s => $"{s.Initials}\t{s.Score}"));
                File.WriteAllText(_config.HighScoreFile, text);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Returns list of high scores from disk, or empty list if file not found.
        /// </summary>
        private List<HighScore> LoadHighScores()
        {
            try
            {
                if (!File.Exists(_config.HighScoreFile))
                    return new List<HighScore>();
                string[] lines = File.ReadAllLines(_config.HighScoreFile);
                List<HighScore> scores = new List<HighScore>();
                foreach (string line in lines)
                {
                    string[] split = line.Trim().ToUpper().Split('\t');
                    if (split.Length != 2)
                        continue;
                    try
                    {
                        string initials = Config.CleanInitials(split[0]);
                        int score = Int32.Parse(split[1]);
                        scores.Add(new HighScore(initials, score));
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.LogError(ex);
                    }
                }
                return scores.OrderByDescending(s => s.Score).Take(10).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
            return new List<HighScore>();
        }
    }

    /// <summary>
    /// Represents a single high score.
    /// </summary>
    public class HighScore
    {
        public string Initials { get; }
        public int Score { get; }

        public HighScore(string initials, int score)
        {
            Initials = initials;
            Score = score;
        }
    }


}

using Bricker.Configuration;
using Bricker.Error;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bricker.Game
{
    /// <summary>
    /// Stores game statistics.
    /// </summary>
    public class GameStats
    {
        //private
        private readonly Config _config;
        private List<HighScore> _highScores;
        private int _score;
        private int _lines;
        private int _level;
        private int _linesSent;
        private bool _gameOver;

        //public
        public IReadOnlyList<HighScore> HighScores => _highScores.AsReadOnly();
        public int Score => _score;
        public int Lines => _lines;
        public int Level => _level;
        public int LinesSent => _linesSent;
        public bool GameOver => _gameOver;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public GameStats(Config config)
        {
            _config = config;
            _highScores = LoadHighScores();
            _score = 0;
            _lines = 0;
            _level = 1;
            _linesSent = 0;
            _gameOver = false;
        }

        /// <summary>
        /// Resets stats for new game.
        /// </summary>
        public void Reset()
        {
            _highScores = LoadHighScores();
            _score = 0;
            _lines = 0;
            _level = 1;
            _linesSent = 0;
            _gameOver = false;
        }

        /// <summary>
        /// Increments current score.
        /// </summary>
        public void IncrementScore(int value)
        {
            _score += value;
        }

        /// <summary>
        /// Increments cleared line count, and sets level.
        /// </summary>
        public void IncrementLines(int value)
        {
            _lines += value;
            _level = (_lines / 25) + 1;
        }

        /// <summary>
        /// Sets the current level.
        /// </summary>
        public void SetLevel(int level)
        {
            if (level > 10)
                level = 10;
            if (level < 1)
                level = 1;
            _level = level;
        }

        /// <summary>
        /// Increments lines sent (two-player mode).
        /// </summary>
        public void IncrementLinesSent(int value)
        {
            _linesSent += value;
        }

        /// <summary>
        /// Sets game-over flag to true (means local player lost).
        /// </summary>
        public void SetGameOver()
        {
            _gameOver = true;
        }

        /// <summary>
        /// Returns true if score can be placed on board.
        /// </summary>
        public bool IsHighScore()
        {
            if (_highScores.Count < 10)
                return true;
            int lowest = Int32.MaxValue;
            foreach (HighScore score in _highScores)
                if (score.Score < lowest)
                    lowest = score.Score;
            return _score > lowest;
        }

        /// <summary>
        /// Adds a new high score, sorts and keeps top 10, saves to disk.
        /// </summary>
        public void AddHighScore(string initials)
        {
            _highScores.Add(new HighScore(initials, _score));
            _highScores = _highScores.OrderByDescending(s => s.Score).Take(10).ToList();
            SaveHighScores();
        }

        /// <summary>
        /// Saves high scores to disk.
        /// </summary>
        private void SaveHighScores()
        {
            try
            {
                string text = String.Join(Environment.NewLine, _highScores.Select(s => $"{s.Initials}\t{s.Score}"));
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

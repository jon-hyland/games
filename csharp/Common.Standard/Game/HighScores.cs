using Common.Standard.Error;
using Common.Standard.Extensions;
using Common.Standard.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common.Standard.Game
{
    public class HighScores
    {
        //private
        private readonly int _maxCount;
        private readonly string _localFile;
        private List<HighScore> _scores;

        //events
        public event Action<HighScore> ScoreAdded;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public HighScores(int maxCount, IEnumerable<HighScore> scores = null, string localFile = null)
        {
            _maxCount = maxCount;
            _localFile = localFile;
            if (scores != null)
                _scores = scores.OrderByDescending(s => s.Score).Take(maxCount).ToList();
            else if (localFile != null)
                _scores = LoadLocalFile(localFile, maxCount);
            else
                _scores = new List<HighScore>();
        }

        /// <summary>
        /// Returns copy of high scores.
        /// </summary>
        public IReadOnlyList<HighScore> GetScores()
        {
            lock (this)
            {
                return _scores.ToList();
            }
        }

        /// <summary>
        /// Replaces high scores with updated list.
        /// </summary>
        public void ReplaceHighScores(IEnumerable<HighScore> scores)
        {
            lock (this)
            {
                _scores = scores.OrderByDescending(s => s.Score).Take(_maxCount).ToList();
            }
        }

        /// <summary>
        /// Returns true if score can be placed on board.
        /// </summary>
        public bool IsHighScore(int score)
        {
            lock (this)
            {
                if (_scores.Count < _maxCount)
                    return true;
                int lowest = Int32.MaxValue;
                foreach (HighScore s in _scores)
                    if (s.Score < lowest)
                        lowest = s.Score;
                return score > lowest;
            }
        }

        /// <summary>
        /// Adds a new high score, sorts and keeps top 10, saves to disk.
        /// </summary>
        public void AddHighScore(string initials, int score)
        {
            HighScore hs = new HighScore(initials, score);
            lock (this)
            {
                _scores.Add(hs);
                _scores = _scores.OrderByDescending(s => s.Score).Take(_maxCount).ToList();
                SaveLocalFile(_localFile, _scores);
            }
            ScoreAdded?.InvokeFromTask(hs);
        }

        /// <summary>
        /// Saves high scores to disk.
        /// </summary>
        private static void SaveLocalFile(string file, List<HighScore> scores)
        {
            try
            {
                if (file == null)
                    return;
                string text = String.Join(Environment.NewLine, scores.Select(s => $"{s.Initials}\t{s.Score}"));
                File.WriteAllText(file, text);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Returns list of high scores from disk, or empty list if file not found.
        /// </summary>
        private static List<HighScore> LoadLocalFile(string file, int maxCount)
        {
            try
            {
                if (file == null)
                    return new List<HighScore>();
                if (!File.Exists(file))
                    return new List<HighScore>();

                string[] lines = File.ReadAllLines(file);
                List<HighScore> scores = new List<HighScore>();
                foreach (string line in lines)
                {
                    string[] split = line.Trim().ToUpper().Split('\t');
                    if (split.Length != 2)
                        continue;
                    try
                    {
                        string initials = CleanInitials(split[0]);
                        int score = Int32.Parse(split[1]);
                        scores.Add(new HighScore(initials, score));
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.LogError(ex);
                    }
                }
                return scores.OrderByDescending(s => s.Score).Take(maxCount).ToList();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
            return new List<HighScore>();
        }

        /// <summary>
        /// Normalizes initials.
        /// </summary>
        private static string CleanInitials(string initials)
        {
            initials = initials.ToUpper().Trim();
            if (initials.Length > 3)
                initials = initials.Substring(0, 3);
            return initials;
        }

        /// <summary>
        /// Serializes high score list to packet bytes.
        /// </summary>
        public byte[] ToBytes()
        {
            PacketBuilder builder = new PacketBuilder();
            lock (this)
            {
                builder.AddUInt16((ushort)_scores.Count);
                foreach (HighScore score in _scores)
                {
                    builder.AddString(score.Initials ?? "");
                    builder.AddInt32(score.Score);
                }
            }
            return builder.ToBytes();
        }

        /// <summary>
        /// Deserializes packet data 
        /// </summary>
        public void UpdateFromBytes(byte[] bytes)
        {
            PacketParser parser = new PacketParser(bytes);
            lock (this)
            {
                _scores.Clear();
                int count = parser.GetUInt16();
                for (int i = 0; i < count; i++)
                {
                    string initials = parser.GetString();
                    int score = parser.GetInt32();
                    _scores.Add(new HighScore(initials, score));
                }
                _scores = _scores.OrderByDescending(s => s.Score).Take(_maxCount).ToList();
            }
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

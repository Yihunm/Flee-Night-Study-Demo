using System;
using System.Collections.Generic;
using UnityEngine;

namespace FleeNightStudy
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public float elapsedSeconds;
        public int textbooksCollected;
        public int score;
        public string difficulty;
        public long timestampUtc;
    }

    public static class GameResultStats
    {
        public static float ElapsedSeconds { get; set; }
        public static int TextbooksCollected { get; set; }
        public static int Score { get; set; }
        public static string PlayerName { get; set; }
        public static GameDifficulty Difficulty { get; set; }
        public static bool WasVictory { get; set; }
        public static string EndReason { get; set; }

        public static void RecordVictory(int collected, float elapsed, GameDifficulty diff, string playerName)
        {
            WasVictory = true;
            EndReason = "胜利";
            TextbooksCollected = collected;
            ElapsedSeconds = elapsed;
            Difficulty = diff;
            PlayerName = playerName;
            Score = CalculateScore(collected, elapsed, true);
            LeaderboardManager.SaveEntry(new LeaderboardEntry
            {
                playerName = playerName,
                elapsedSeconds = elapsed,
                textbooksCollected = collected,
                score = Score,
                difficulty = diff.ToString(),
                timestampUtc = DateTime.UtcNow.Ticks
            });
        }

        public static void RecordDefeat(int collected, float elapsed, GameDifficulty diff, string playerName, string reason)
        {
            WasVictory = false;
            EndReason = reason;
            TextbooksCollected = collected;
            ElapsedSeconds = elapsed;
            Difficulty = diff;
            PlayerName = playerName;
            Score = CalculateScore(collected, elapsed, false);
        }

        static int CalculateScore(int collected, float elapsed, bool victory)
        {
            int baseScore = collected * 120;
            int timeBonus = victory ? Mathf.RoundToInt(Mathf.Max(0f, 480f - elapsed) * 1.5f) : 0;
            return baseScore + timeBonus;
        }
    }

    public static class LeaderboardManager
    {
        const string PrefKey = "FleeNightStudy_Leaderboard";
        public const int TopDisplayCount = 10;

        public static List<LeaderboardEntry> LoadAll()
        {
            if (!PlayerPrefs.HasKey(PrefKey))
                return new List<LeaderboardEntry>();
            try
            {
                var wrap = JsonUtility.FromJson<LeaderboardWrap>(PlayerPrefs.GetString(PrefKey));
                return wrap?.entries ?? new List<LeaderboardEntry>();
            }
            catch
            {
                return new List<LeaderboardEntry>();
            }
        }

        public static void SaveEntry(LeaderboardEntry entry)
        {
            var list = LoadAll();
            list.Add(entry);
            list = KeepTopEntriesPerDifficulty(list, TopDisplayCount);
            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(new LeaderboardWrap { entries = list }));
            PlayerPrefs.Save();
        }

        static List<LeaderboardEntry> KeepTopEntriesPerDifficulty(List<LeaderboardEntry> entries, int maxPerDifficulty)
        {
            var kept = new List<LeaderboardEntry>();
            kept.AddRange(KeepTopForDifficulty(entries, GameDifficulty.Easy, maxPerDifficulty));
            kept.AddRange(KeepTopForDifficulty(entries, GameDifficulty.Normal, maxPerDifficulty));
            return kept;
        }

        static List<LeaderboardEntry> KeepTopForDifficulty(
            List<LeaderboardEntry> entries,
            GameDifficulty difficulty,
            int maxCount)
        {
            var subset = entries.FindAll(e => MatchesDifficulty(e, difficulty));
            subset.Sort((a, b) => a.elapsedSeconds.CompareTo(b.elapsedSeconds));
            if (subset.Count > maxCount)
                subset.RemoveRange(maxCount, subset.Count - maxCount);
            return subset;
        }

        public static List<LeaderboardEntry> GetTopEntries(GameDifficulty? difficulty = null, int count = TopDisplayCount)
        {
            var list = LoadAll();
            if (difficulty.HasValue)
                list = list.FindAll(e => MatchesDifficulty(e, difficulty.Value));

            list.Sort((a, b) => a.elapsedSeconds.CompareTo(b.elapsedSeconds));
            if (list.Count > count)
                list = list.GetRange(0, count);
            return list;
        }

        public static string FormatLeaderboardText(GameDifficulty? difficulty = null)
        {
            var list = GetTopEntries(difficulty, TopDisplayCount);

            if (list.Count == 0)
            {
                if (difficulty.HasValue)
                    return $"{FormatDifficultyLabel(difficulty.Value)}模式暂无排名记录";
                return "暂无排名记录";
            }

            var header = difficulty.HasValue
                ? $"—— {FormatDifficultyLabel(difficulty.Value)}模式 · 前{TopDisplayCount}名 ——"
                : $"—— 用时排行榜 · 前{TopDisplayCount}名 ——";
            var lines = new List<string> { header };
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                int min = (int)(e.elapsedSeconds / 60f);
                int sec = (int)(e.elapsedSeconds % 60f);
                lines.Add($"{i + 1}. {e.playerName}  {min:00}:{sec:00}");
            }
            return string.Join("\n", lines);
        }

        static bool MatchesDifficulty(LeaderboardEntry entry, GameDifficulty difficulty)
        {
            if (entry == null || string.IsNullOrEmpty(entry.difficulty))
                return false;

            var value = entry.difficulty.Trim();
            if (value == difficulty.ToString())
                return true;

            return difficulty switch
            {
                GameDifficulty.Easy => value is "简单" or "0",
                GameDifficulty.Normal => value is "普通" or "1",
                _ => false
            };
        }

        static string FormatDifficultyLabel(GameDifficulty difficulty)
        {
            return difficulty == GameDifficulty.Easy ? "简单" : "普通";
        }

        [Serializable]
        class LeaderboardWrap
        {
            public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        }
    }
}

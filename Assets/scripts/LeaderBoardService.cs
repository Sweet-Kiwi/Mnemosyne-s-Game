using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
    public string name;
    public int score;
    public int bestStreak;
    public string dateISO; // "2025-08-08"
}

[Serializable]
class LeaderboardData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

public static class LeaderboardService
{
    static string PathFile => System.IO.Path.Combine(Application.persistentDataPath, "leaderboard.json");
    static LeaderboardData cache;

    static void EnsureLoaded()
    {
        if (cache != null) return;
        if (File.Exists(PathFile))
        {
            var json = File.ReadAllText(PathFile);
            cache = JsonUtility.FromJson<LeaderboardData>(json);
            if (cache == null) cache = new LeaderboardData();
        }
        else
        {
            cache = new LeaderboardData();
        }
    }

    public static void AddEntry(LeaderboardEntry entry)
    {
        EnsureLoaded();
        cache.entries.Add(entry);
        // sort by score desc, then bestStreak desc, then recent date
        cache.entries.Sort((a, b) =>
        {
            int cmp = b.score.CompareTo(a.score);
            if (cmp != 0) return cmp;
            cmp = b.bestStreak.CompareTo(a.bestStreak);
            if (cmp != 0) return cmp;
            return string.Compare(b.dateISO, a.dateISO, StringComparison.Ordinal);
        });
        // keep top 100 to stop infinite growth
        if (cache.entries.Count > 100) cache.entries.RemoveRange(100, cache.entries.Count - 100);
        Save();
    }

    public static List<LeaderboardEntry> GetTop(int n)
    {
        EnsureLoaded();
        int take = Mathf.Clamp(n, 1, cache.entries.Count);
        return cache.entries.GetRange(0, take);
    }

    static void Save()
    {
        try
        {
            var json = JsonUtility.ToJson(cache, true);
            File.WriteAllText(PathFile, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to save leaderboard: " + e.Message);
        }
    }
}

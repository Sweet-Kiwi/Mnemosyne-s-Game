using System;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_WEBGL
using System.IO;
#endif

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
#if UNITY_WEBGL
    const string PrefKey = "LEADERBOARD_JSON";
#else
    static string PathFile => System.IO.Path.Combine(Application.persistentDataPath, "leaderboard.json");
#endif

    static LeaderboardData cache;

    static void EnsureLoaded()
    {
        if (cache != null) return;

#if UNITY_WEBGL
        var json = PlayerPrefs.GetString(PrefKey, string.Empty);
        cache = string.IsNullOrEmpty(json) ? new LeaderboardData() : JsonUtility.FromJson<LeaderboardData>(json);
#else
        if (File.Exists(PathFile))
        {
            try
            {
                var json = File.ReadAllText(PathFile);
                cache = JsonUtility.FromJson<LeaderboardData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to read leaderboard file: " + e.Message);
                cache = new LeaderboardData();
            }
        }
        else
        {
            cache = new LeaderboardData();
        }
#endif
        if (cache == null) cache = new LeaderboardData();
        if (cache.entries == null) cache.entries = new List<LeaderboardEntry>();
    }

    public static void AddEntry(LeaderboardEntry entry)
    {
        EnsureLoaded();
        cache.entries.Add(entry);

        // Sort by score desc, then bestStreak desc, then most recent date
        cache.entries.Sort((a, b) =>
        {
            int cmp = b.score.CompareTo(a.score);
            if (cmp != 0) return cmp;
            cmp = b.bestStreak.CompareTo(a.bestStreak);
            if (cmp != 0) return cmp;
            return string.CompareOrdinal(b.dateISO, a.dateISO);
        });

        // keep top 100
        if (cache.entries.Count > 100)
            cache.entries.RemoveRange(100, cache.entries.Count - 100);

        Save();
    }

    public static List<LeaderboardEntry> GetTop(int n)
    {
        EnsureLoaded();
        if (cache.entries.Count == 0) return new List<LeaderboardEntry>();
        int take = Mathf.Clamp(n, 1, cache.entries.Count);
        return cache.entries.GetRange(0, take);
    }

    public static void ClearAll()
    {
        cache = new LeaderboardData();
        Save();
    }

    static void Save()
    {
        try
        {
#if UNITY_WEBGL
            var json = JsonUtility.ToJson(cache, true);
            PlayerPrefs.SetString(PrefKey, json);
            PlayerPrefs.Save();
#else
            var json = JsonUtility.ToJson(cache, true);
            File.WriteAllText(PathFile, json);
#endif
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to save leaderboard: " + e.Message);
        }
    }
}

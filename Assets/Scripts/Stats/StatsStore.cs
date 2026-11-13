using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>Одна клавиша и счётчики по ней (для тепловой карты).</summary>
[Serializable]
public class KeyStat
{
    public string key;
    public int attempts;
    public int errors;
}

/// <summary>Один завершённый заход печати.</summary>
[Serializable]
public class TypingSessionResult
{
    public string mode = "free";
    public string dateIso = "";
    public double seconds;
    public int correctKeystrokes;
    public int errorKeystrokes;
    public int textLength;
    public double cpm;
    public double netCpm;
    public double wpm;
    public double netWpm;
    public double accuracy;
    public List<KeyStat> keys = new List<KeyStat>();
}

/// <summary>
/// Хранилище статистики в Application.persistentDataPath/typing_stats.json.
/// Окно «Статистика» потом читает отсюда всё: средние значения, рекорды, тепловую карту, графики.
/// </summary>
public static class StatsStore
{
    private const int MaxSessions = 300;

    [Serializable]
    public class StatsData
    {
        public List<TypingSessionResult> sessions = new List<TypingSessionResult>();
        public List<KeyStat> totalKeys = new List<KeyStat>();
    }

    private static StatsData data;

    public static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "typing_stats.json"); }
    }

    public static StatsData Data
    {
        get
        {
            if (data == null)
                Load();
            return data;
        }
    }

    public static void Load()
    {
        data = new StatsData();
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                StatsData parsed = JsonUtility.FromJson<StatsData>(json);
                if (parsed != null)
                {
                    if (parsed.sessions == null)
                        parsed.sessions = new List<TypingSessionResult>();
                    if (parsed.totalKeys == null)
                        parsed.totalKeys = new List<KeyStat>();
                    data = parsed;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("StatsStore: не удалось прочитать статистику — " + e.Message);
        }
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true), Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogWarning("StatsStore: не удалось сохранить статистику — " + e.Message);
        }
    }

    public static void AddSession(TypingSessionResult session)
    {
        if (session == null)
            return;

        Data.sessions.Add(session);
        if (Data.sessions.Count > MaxSessions)
            Data.sessions.RemoveRange(0, Data.sessions.Count - MaxSessions);

        MergeKeys(session.keys, Data.totalKeys);
        Save();
    }

    public static int SessionCount
    {
        get { return Data.sessions.Count; }
    }

    public static TypingSessionResult LastSession
    {
        get { return Data.sessions.Count == 0 ? null : Data.sessions[Data.sessions.Count - 1]; }
    }

    public static List<TypingSessionResult> All()
    {
        return Data.sessions;
    }

    /// <summary>Сводка по всем клавишам за всё время (для тепловой карты).</summary>
    public static List<KeyStat> TotalKeys
    {
        get { return Data.totalKeys; }
    }

    /// <summary>Полностью очистить статистику ( sessions и сводку по клавишам).</summary>
    public static void ResetAll()
    {
        data = new StatsData();
        Save();
    }

    /// <summary>Средние значения по всем сессиям (для окна статистики).</summary>
    public static void GetAverages(out double cpm, out double netCpm, out double accuracy, out int errors)
    {
        cpm = 0; netCpm = 0; accuracy = 0; errors = 0;

        var sessions = Data.sessions;
        if (sessions.Count == 0)
            return;

        double sumCpm = 0, sumNet = 0, sumAcc = 0;
        int sumErrors = 0;

        for (int i = 0; i < sessions.Count; i++)
        {
            sumCpm += sessions[i].cpm;
            sumNet += sessions[i].netCpm;
            sumAcc += sessions[i].accuracy;
            sumErrors += sessions[i].errorKeystrokes;
        }

        cpm = sumCpm / sessions.Count;
        netCpm = sumNet / sessions.Count;
        accuracy = sumAcc / sessions.Count;
        errors = sumErrors;
    }

    /// <summary>Личные рекорды: лучшая скорость и лучшая точность за всё время.</summary>
    public static void GetRecords(out double bestCpm, out double bestAccuracy)
    {
        bestCpm = 0;
        bestAccuracy = 0;

        var sessions = Data.sessions;
        for (int i = 0; i < sessions.Count; i++)
        {
            if (sessions[i].cpm > bestCpm)
                bestCpm = sessions[i].cpm;
            if (sessions[i].accuracy > bestAccuracy)
                bestAccuracy = sessions[i].accuracy;
        }
    }

    /// <summary>Топ клавиш по числу ошибок (для «топ проблемных клавиш» и тепловой карты).</summary>
    public static List<KeyStat> GetProblemKeys(int count)
    {
        var list = new List<KeyStat>(Data.totalKeys);
        list.Sort(delegate (KeyStat a, KeyStat b)
        {
            // Больше ошибок — выше; при равенстве чаще используемая клавиша
            int byErrors = b.errors.CompareTo(a.errors);
            if (byErrors != 0)
                return byErrors;
            return b.attempts.CompareTo(a.attempts);
        });

        if (list.Count > count)
            list.RemoveRange(count, list.Count - count);

        return list;
    }

    private static void MergeKeys(List<KeyStat> from, List<KeyStat> into)
    {
        if (from == null)
            return;

        for (int i = 0; i < from.Count; i++)
        {
            KeyStat src = from[i];
            if (string.IsNullOrEmpty(src.key))
                continue;

            KeyStat target = null;
            for (int j = 0; j < into.Count; j++)
            {
                if (into[j].key == src.key)
                {
                    target = into[j];
                    break;
                }
            }

            if (target == null)
            {
                target = new KeyStat { key = src.key };
                into.Add(target);
            }

            target.attempts += src.attempts;
            target.errors += src.errors;
        }
    }
}

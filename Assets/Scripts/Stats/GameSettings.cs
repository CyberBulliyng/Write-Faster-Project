using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Настройки игры. Лежат рядом со статистикой: persistentDataPath/typing_settings.json
/// </summary>
public static class GameSettings
{
    public const int MinFontSize = 22;
    public const int MaxFontSize = 44;

    [Serializable]
    public class SettingsData
    {
        // Размер шрифта строк набора (влияет на длину строки и сколько текста видно)
        public int typingFontSize = 34;

        // Нужно ли жать Enter в конце абзаца, или абзац перескакивается сам
        public bool requireParagraphEnter = true;
    }

    private static SettingsData data;

    public static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "typing_settings.json"); }
    }

    public static SettingsData Data
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
        data = new SettingsData();
        try
        {
            if (File.Exists(FilePath))
            {
                SettingsData parsed = JsonUtility.FromJson<SettingsData>(File.ReadAllText(FilePath, Encoding.UTF8));
                if (parsed != null)
                    data = parsed;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameSettings: файл настроек не прочитался — " + e.Message);
        }

        ClampAll();
    }

    public static void Save()
    {
        try
        {
            ClampAll();
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true), Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameSettings: не удалось сохранить — " + e.Message);
        }
    }

    public static int FontSize
    {
        get { return Data.typingFontSize; }
    }

    public static bool RequireParagraphEnter
    {
        get { return Data.requireParagraphEnter; }
    }

    public static void SetFontSize(int size)
    {
        Data.typingFontSize = size;
        Save();
    }

    public static void SetRequireParagraphEnter(bool value)
    {
        Data.requireParagraphEnter = value;
        Save();
    }

    private static void ClampAll()
    {
        if (Data.typingFontSize < MinFontSize)
            Data.typingFontSize = MinFontSize;
        if (Data.typingFontSize > MaxFontSize)
            Data.typingFontSize = MaxFontSize;
    }
}

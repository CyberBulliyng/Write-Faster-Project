using System;

/// <summary>Параметры одного уровня игрового режима.</summary>
public class LevelInfo
{
    public int level;
    public int minWordLength;
    public int maxWordLength;
    public int enemyCount;
    public float enemySpeed;
    public float spawnInterval;
    public int maxAliveEnemies;
    public bool isBoss;
    public int bossWordCount;
    public float bossSpeed;
}

/// <summary>
/// Расчёт сложности. С каждым уровнем растёт что-то одно:
/// на чётных уровнях прибавляется враг, на нечётных — длина слов.
/// Каждый пятый уровень — босс.
/// </summary>
public static class LevelRules
{
    public const int BossEvery = 5;

    public static LevelInfo Get(int level)
    {
        int lengthStep = (level - 1) / 2;   // нечётные уровни удлинняют слова
        int enemyStep = level / 2;          // чётные уровни добавляют врагов

        var info = new LevelInfo
        {
            level = level,
            minWordLength = Clamp(3 + lengthStep, 3, 8),
            maxWordLength = Clamp(4 + lengthStep, 4, 10),
            enemyCount = Clamp(6 + enemyStep, 6, 20),
            enemySpeed = 0.55f + 0.04f * (level - 1),
            spawnInterval = Math.Max(0.7f, 2.4f - 0.08f * (level - 1)),
            maxAliveEnemies = Clamp(3 + level / 3, 3, 8),
            isBoss = level % BossEvery == 0,
            bossSpeed = 0.2f
        };

        info.enemySpeed = Math.Min(info.enemySpeed, 1.25f);

        // Босс заменяет волну обычных врагов: один большой шар с несколькими словами
        if (info.isBoss)
        {
            info.enemyCount = 1;
            info.bossWordCount = 2 + level / BossEvery;
            info.maxAliveEnemies = 1;
            info.minWordLength = Clamp(4 + lengthStep, 4, 8);
            info.maxWordLength = Clamp(6 + lengthStep, 5, 11);
        }

        return info;
    }

    /// <summary>Очки за уничтожение: зависят от длины слова.</summary>
    public static int ScoreForWord(int wordLength, bool boss)
    {
        return boss ? 50 * wordLength : 10 * wordLength;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

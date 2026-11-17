using System.Collections.Generic;

/// <summary>
/// Словарь слов для игрового режима. Слова лежат одним списком,
/// а раскладываются по длине автоматически — так нельзя ошибиться в длине.
/// </summary>
public static class WordLibrary
{
    private static Dictionary<int, List<string>> byLength;

    private static readonly string[] all =
    {
        // 3 буквы
        "дом", "кот", "лес", "мир", "сон", "год", "раз", "нос", "рот", "пар",
        "луч", "мяч", "путь", "конь", "зуб", "сыр", "лев", "фон", "код", "час",
        // 4 буквы
        "день", "ночь", "рука", "нога", "глаз", "стол", "стул", "окно", "вода", "река",
        "зима", "лето", "друг", "рыба", "край", "гора", "море", "трава", "поле", "дождь",
        "снег", "огонь", "поезд", "кошка", "мышка", "кадр", "лист", "кора", "мост", "путь",
        // 5 букв
        "книга", "комната", "слово", "язык", "мысль", "человек", "город", "деревня",
        "письмо", "окно", "мороз", "ветер", "облако", "дорога", "работа", "учёба",
        "курсор", "клавиша", "экран", "пауза", "уровень", "враг", "шарик", "точка",
        // 6 букв
        "клавиша", "скорость", "точность", "ошибка", "результат", "память", "внимание",
        "пробел", "знак", "текст", "строчка", "символ", "программа", "проект", "задача",
        "решение", "движение", "полёт", "ракета", "мишень", "прицел", "щит", "броня",
        // 7 букв
        "линия", "фигура", "размер", "преграда", "секунда", "минута", "финал", "приз",
        "награда", "сложность", "уровень", "босс", "здоровье", "жизнь", "удар", "защита",
        // 8 букв
        "противник", "стрелок", "мишень", "препятствие", "комбинация", "последовательность",
        "упражнение", "тренировка", "навык", "ритм", "температура", "напряжение",
        // 9-10 букв
        "выполнение", "прогресс", "достижение", "вычисление", "попадание", "уничтожение",
        "расстояние", "быстрота", "точнейший", "настоящий", "постоянно", "постепенно"
    };

    /// <summary>Все слова длиной от min до max включительно.</summary>
    public static List<string> GetWordsInRange(int min, int max)
    {
        EnsureBuilt();

        var result = new List<string>();
        for (int len = min; len <= max; len++)
        {
            List<string> bucket;
            if (byLength.TryGetValue(len, out bucket))
                result.AddRange(bucket);
        }

        return result;
    }

    /// <summary>Случайное слово заданной длины (если таких слов нет — ближайшая доступная длина).</summary>
    public static string GetRandom(int min, int max, System.Random random)
    {
        List<string> pool = GetWordsInRange(min, max);
        if (pool.Count == 0)
            pool = GetWordsInRange(3, 12);

        if (pool.Count == 0)
            return "дом";

        return pool[random.Next(0, pool.Count)];
    }

    private static void EnsureBuilt()
    {
        if (byLength != null)
            return;

        byLength = new Dictionary<int, List<string>>();

        for (int i = 0; i < all.Length; i++)
        {
            string word = all[i];
            if (string.IsNullOrEmpty(word))
                continue;

            int len = word.Length;
            List<string> bucket;
            if (!byLength.TryGetValue(len, out bucket))
            {
                bucket = new List<string>();
                byLength[len] = bucket;
            }

            if (!bucket.Contains(word))
                bucket.Add(word);
        }
    }
}

using System;
using System.Collections.Generic;

/// <summary>Одна строка набираемого текста и то, что в ней уже напечатано.</summary>
public class TypingLine
{
    public string text = string.Empty;

    // После последней буквы этой строки нужно нажать Enter (конец абзаца)
    public bool requiresEnter;

    // Что игрок нажал на каждой позиции ('\0' — позицию ещё не проходили)
    public char[] typed = new char[0];

    public bool enterTyped;
}

/// <summary>
/// Модель набираемого текста: курсор, посимвольная проверка, счётчики ошибок.
/// Чистая логика без Unity — легко читать и переиспользовать в других режимах.
/// </summary>
public class TypingTextModel
{
    public const char EnterChar = '\n';
    public const char BackspaceChar = '\b';

    private readonly List<TypingLine> lines = new List<TypingLine>();
    public IReadOnlyList<TypingLine> Lines { get { return lines; } }

    public int Line { get; private set; }
    public int Col { get; private set; }
    public bool Completed { get; private set; }

    public int CorrectKeystrokes { get; private set; }
    public int ErrorKeystrokes { get; private set; }

    /// <summary>Сколько раз нажимали на клавишу (для тепловой карты) — по ожидаемому символу.</summary>
    public readonly Dictionary<char, int> AttemptsByKey = new Dictionary<char, int>();

    /// <summary>Сколько раз ошиблись на клавише.</summary>
    public readonly Dictionary<char, int> ErrorsByKey = new Dictionary<char, int>();

    /// <summary>Вызывается, когда весь текст набран.</summary>
    public event Action completed;

    public void Load(string rawText, int maxCharsPerLine)
    {
        lines.Clear();
        lines.AddRange(TextWrapper.Wrap(rawText, maxCharsPerLine));
        Restart();
    }

    /// <summary>Обнулить прогресс и счётчики, оставив тот же текст.</summary>
    public void Restart()
    {
        Line = 0;
        Col = 0;
        Completed = false;
        CorrectKeystrokes = 0;
        ErrorKeystrokes = 0;
        AttemptsByKey.Clear();
        ErrorsByKey.Clear();

        for (int i = 0; i < lines.Count; i++)
        {
            Array.Clear(lines[i].typed, 0, lines[i].typed.Length);
            lines[i].enterTyped = false;
        }
    }

    public int TotalPositions
    {
        get
        {
            int total = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                total += lines[i].text.Length;
                if (lines[i].requiresEnter)
                    total++;
            }
            return total;
        }
    }

    public int ResolvedPositions
    {
        get
        {
            int resolved = 0;
            for (int i = 0; i < lines.Count && i <= Line; i++)
            {
                if (i < Line)
                {
                    resolved += lines[i].text.Length;
                    if (lines[i].requiresEnter)
                        resolved++;
                }
                else
                {
                    resolved += Col;
                    if (lines[i].enterTyped)
                        resolved++;
                }
            }
            return resolved;
        }
    }

    public float Progress
    {
        get
        {
            int total = TotalPositions;
            if (total == 0)
                return 1f;
            return Mathf01((float)ResolvedPositions / total);
        }
    }

    private static float Mathf01(float v)
    {
        if (v < 0f) return 0f;
        if (v > 1f) return 1f;
        return v;
    }

    public float Accuracy
    {
        get
        {
            int total = CorrectKeystrokes + ErrorKeystrokes;
            if (total == 0)
                return 1f;
            return (float)CorrectKeystrokes / total;
        }
    }

    /// <summary>Обработанный символ: буква, пробел, знак препинания, Enter или Backspace.</summary>
    public void Process(char c)
    {
        if (Completed)
            return;

        if (c == BackspaceChar)
        {
            Backspace();
            return;
        }

        if (lines.Count == 0)
            return;

        NormalizeCursor();
        if (Completed)
            return;

        TypingLine line = lines[Line];

        if (Col < line.text.Length)
        {
            char expected = line.text[Col];
            bool correct = char.ToUpperInvariant(c) == char.ToUpperInvariant(expected);

            CountKey(expected, correct);
            if (correct)
            {
                // Верно: запоминаем ожидаемый символ (чтобы регистр не ломал вид строки)
                line.typed[Col] = expected;
                CorrectKeystrokes++;
                Col++;
            }
            else
            {
                // Ошибка: запоминаем то, что реально нажали, но НЕ двигаемся вперёд,
                // пока игрок не наберёт нужный символ. Так текст не "разъезжается".
                if (!char.IsControl(c))
                    line.typed[Col] = c;
                ErrorKeystrokes++;
            }
        }
        else if (line.requiresEnter && !line.enterTyped)
        {
            bool correct = c == EnterChar;
            CountKey(EnterChar, correct);
            if (correct)
            {
                line.enterTyped = true;
                CorrectKeystrokes++;
            }
            else
            {
                ErrorKeystrokes++;
            }
        }

        NormalizeCursor();
    }

    /// <summary>Шаг назад: стираем последний введённый символ (счётчики ошибок не уменьшаем).</summary>
    public void Backspace()
    {
        if (Completed || lines.Count == 0)
            return;

        TypingLine line = lines[Line];

        if (line.enterTyped)
        {
            line.enterTyped = false;
            return;
        }

        if (Col > 0)
        {
            Col--;
            line.typed[Col] = '\0';
            return;
        }

        if (Line == 0)
            return;

        // Переходим на предыдущую строку и стираем её последний символ
        Line--;
        TypingLine prev = lines[Line];
        if (prev.enterTyped)
        {
            prev.enterTyped = false;
            Col = prev.text.Length;
            return;
        }

        if (prev.text.Length > 0)
        {
            Col = prev.text.Length - 1;
            prev.typed[Col] = '\0';
        }
        else
        {
            Col = 0;
        }
    }

    /// <summary>Символ, который от игрока ждут прямо сейчас.</summary>
    public bool TryGetExpected(out char expected)
    {
        expected = '\0';
        if (Completed || lines.Count == 0)
            return false;

        NormalizeCursor();
        if (Completed || Line >= lines.Count)
            return false;

        TypingLine line = lines[Line];
        if (Col < line.text.Length)
        {
            expected = line.text[Col];
            return true;
        }

        if (line.requiresEnter && !line.enterTyped)
        {
            expected = EnterChar;
            return true;
        }

        return false;
    }

    /// <summary>Переставляет курсор на следующую позицию, которую ещё не набрали.</summary>
    private void NormalizeCursor()
    {
        while (lines.Count > 0)
        {
            if (Line >= lines.Count)
            {
                Complete();
                return;
            }

            TypingLine line = lines[Line];
            bool pending = Col < line.text.Length ||
                           (line.requiresEnter && !line.enterTyped);

            if (pending)
                return;

            // Строка закрыта (мягкий перенос) — без нажатия клавиши идём дальше
            Line++;
            Col = 0;
        }
    }

    private void Complete()
    {
        if (Completed)
            return;

        Completed = true;
        completed?.Invoke();
    }

    private void CountKey(char expected, bool correct)
    {
        char key = char.ToUpperInvariant(expected) == expected && char.IsLetter(expected)
            ? expected
            : char.ToLowerInvariant(expected);

        AttemptsByKey[key] = AttemptsByKey.TryGetValue(key, out int attempts) ? attempts + 1 : 1;

        if (!correct)
            ErrorsByKey[key] = ErrorsByKey.TryGetValue(key, out int errors) ? errors + 1 : 1;
    }
}

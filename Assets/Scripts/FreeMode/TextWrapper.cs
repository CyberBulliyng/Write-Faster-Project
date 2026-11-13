using System;

/// <summary>
/// Разбивает длинный текст на строки для отображения.
/// Переносит по пробелам (word wrap), пустые абзацы убирает.
/// </summary>
public static class TextWrapper
{
    /// <summary>
    /// Возвращает список строк. Каждый абзац исходного текста — это одна или несколько строк,
    /// последняя строка абзаца помечается requiresEnter (нужно нажать Enter).
    /// </summary>
    public static System.Collections.Generic.List<TypingLine> Wrap(string rawText, int maxCharsPerLine)
    {
        var result = new System.Collections.Generic.List<TypingLine>();
        if (string.IsNullOrEmpty(rawText))
            return result;

        if (maxCharsPerLine < 10)
            maxCharsPerLine = 10;

        // Абзацы из исходного текста
        string[] paragraphs = rawText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        foreach (string paragraph in paragraphs)
        {
            string text = paragraph.Trim();
            if (text.Length == 0)
                continue; // пустые строки набирать не нужно

            int before = result.Count;
            WrapParagraph(text, maxCharsPerLine, result);

            // Помечаем конец абзаца: после этой строки игрок жмёт Enter
            if (result.Count > before)
                result[result.Count - 1].requiresEnter = true;
        }

        // В самом конце текста Enter не нужен
        if (result.Count > 0)
            result[result.Count - 1].requiresEnter = false;

        return result;
    }

    private static void WrapParagraph(string text, int maxChars, System.Collections.Generic.List<TypingLine> output)
    {
        int start = 0; // начало текущей строки

        while (start < text.Length)
        {
            // Осталось меньше, чем помещается
            if (text.Length - start <= maxChars)
            {
                output.Add(CreateLine(text.Substring(start)));
                return;
            }

            int limit = start + maxChars;

            // Ищем последний пробел в пределах строки
            int breakAt = text.LastIndexOf(' ', limit, maxChars);
            if (breakAt <= start)
            {
                // Слово длиннее строки — режем по символам
                breakAt = limit;
            }

            string line = text.Substring(start, breakAt - start).TrimEnd();
            if (line.Length == 0)
            {
                // Защита от бесконечного цикла на череде пробелов
                line = text.Substring(start, Math.Min(maxChars, text.Length - start));
                breakAt = start + line.Length;
            }

            output.Add(CreateLine(line));
            start = breakAt;
            while (start < text.Length && text[start] == ' ')
                start++;
        }
    }

    private static TypingLine CreateLine(string text)
    {
        return new TypingLine
        {
            text = text,
            requiresEnter = false,
            typed = new char[text.Length],
            enterTyped = false
        };
    }
}

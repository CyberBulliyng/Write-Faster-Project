using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Получает введённый символ от CheckEnterkeyboard,
/// показывает его на экране и проверяет каждое слово слева направо.
/// Введённая (правильная) часть слова подсвечивается цветом.
/// </summary>
public class WordChecker : MonoBehaviour
{
    [Header("Вывод последнего введённого символа (можно оставить пустым)")]
    public TMP_Text typedCharDisplay;

    [Header("Слова на экране (TMP тексты)")]
    public List<TMP_Text> wordTexts = new List<TMP_Text>();

    [Header("Цвета подсветки")]
    public Color typedColor = new Color(0f, 1f, 0.4f);   // уже введённая часть
    public Color baseColor = Color.white;                // остальная часть

    /// <summary>Событие: слово полностью набрано (текст, само слово)</summary>
    public event Action<TMP_Text, string> wordCompleted;

    // Исходные слова без разметки
    private readonly List<string> words = new List<string>();
    // Сколько символов уже правильно введено в каждом слове
    private readonly List<int> progress = new List<int>();
    // Слова, которые уже набраны полностью
    private readonly List<bool> completed = new List<bool>();
    // Индекс слова, которое вводится прямо сейчас (-1 — ни одного)
    private int activeIndex = -1;

    private void Start()
    {
        // Запоминаем исходный текст каждого слова
        words.Clear();
        progress.Clear();
        completed.Clear();

        for (int i = 0; i < wordTexts.Count; i++)
        {
            if (wordTexts[i] == null)
            {
                words.Add(string.Empty);
                progress.Add(0);
                completed.Add(true); // пустой слот не трогаем
                continue;
            }

            // Чистим от возможной rich-text разметки
            string plain = StripTags(wordTexts[i].text).Trim();
            words.Add(plain);
            progress.Add(0);
            completed.Add(plain.Length == 0);
            RedrawWord(i);
        }

        if (typedCharDisplay != null)
            typedCharDisplay.text = string.Empty;

        // Подписываемся на ввод (в Start — instance уже точно создан)
        CheckEnterkeyboard.instance.enterNewCharacter += OnCharacterTyped;
    }

    private void OnDestroy()
    {
        if (CheckEnterkeyboard.instance != null)
            CheckEnterkeyboard.instance.enterNewCharacter -= OnCharacterTyped;
    }

    private void OnCharacterTyped(char c)
    {
        // Enter и Backspace — не буквы, в игровом режиме их не проверяем
        if (c == '\n' || c == '\b')
            return;

        // 1. Показываем введённый символ на экране
        if (typedCharDisplay != null)
            typedCharDisplay.text = c.ToString();

        char lower = char.ToLowerInvariant(c);

        if (activeIndex >= 0)
        {
            // 2. Слово уже вводится — работаем только с ним
            string word = words[activeIndex];
            int pos = progress[activeIndex];

            if (pos < word.Length && char.ToLowerInvariant(word[pos]) == lower)
            {
                // Верно: двигаем прогресс
                progress[activeIndex]++;
                RedrawWord(activeIndex);

                // Слово набрано целиком — удаляем и освобождаем ввод
                if (progress[activeIndex] >= word.Length)
                {
                    completed[activeIndex] = true;
                    wordCompleted?.Invoke(wordTexts[activeIndex], word);
                    RemoveWord(activeIndex);
                    activeIndex = -1;
                }
            }
            else
            {
                // Ошибка в порядке букв — сбрасываем прогресс этого слова,
                // ввод снова свободен (никакое другое слово не трогаем)
                progress[activeIndex] = 0;
                RedrawWord(activeIndex);
                activeIndex = -1;
            }
        }
        else
        {
            // 3. Ни одно слово не вводится — ищем, чья первая буква совпала
            for (int i = 0; i < words.Count; i++)
            {
                if (completed[i])
                    continue;

                string word = words[i];
                if (word.Length > 0 && char.ToLowerInvariant(word[0]) == lower)
                {
                    // Это слово становится единственным активным
                    activeIndex = i;
                    progress[i] = 1;
                    RedrawWord(i);
                    break; // остальные слова не трогаем
                }
            }
            // Нет совпадения с первой буквой — символ просто игнорируется
        }
    }

    /// <summary>Удаляет набранное слово с экрана (скрывает объект).</summary>
    private void RemoveWord(int i)
    {
        if (wordTexts[i] != null)
        {
            wordTexts[i].gameObject.SetActive(false);
        }
    }

    /// <summary>Перерисовывает слово с подсветкой введённой части.</summary>
    private void RedrawWord(int i)
    {
        if (wordTexts[i] == null)
            return;

        string word = words[i];
        int pos = completed[i] ? word.Length : progress[i];

        string typed = word.Substring(0, pos);
        string rest = word.Substring(pos);

        string typedHex = ColorUtility.ToHtmlStringRGB(typedColor);
        string baseHex = ColorUtility.ToHtmlStringRGB(baseColor);

        wordTexts[i].text =
            $"<color=#{typedHex}>{typed}</color>" +
            $"<color=#{baseHex}>{rest}</color>";
    }

    /// <summary>Убирает rich-text теги из строки (простой вариант: &lt;...&gt;).</summary>
    private static string StripTags(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        var sb = new System.Text.StringBuilder();
        bool inside = false;
        foreach (char ch in s)
        {
            if (ch == '<') inside = true;
            else if (ch == '>') inside = false;
            else if (!inside) sb.Append(ch);
        }
        return sb.ToString();
    }
}

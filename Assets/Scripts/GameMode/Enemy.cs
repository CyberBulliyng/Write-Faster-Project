using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum EnemyStep
{
    Nothing,
    Correct,
    Wrong,
    WordFinished,
    EnemyKilled
}

/// <summary>
/// Враг: круг, который едет к игроку, и слово над ним.
/// Обычный враг умирает от одного слова, босс — от нескольких подряд.
/// </summary>
public class Enemy : MonoBehaviour
{
    public bool IsBoss { get; private set; }
    public float Speed { get; set; }
    public bool Alive { get; private set; }
    public bool Frozen { get; set; }
    public bool Highlighted { get; private set; }

    /// <summary>Расстояние, на котором враг считается добравшимся до игрока.</summary>
    public float HitDistance { get; private set; }

    /// <summary>Враг дошёл до игрока.</summary>
    public event Action<Enemy> reachedPlayer;

    /// <summary>Враг уничтожен набором (после короткой анимации).</summary>
    public event Action<Enemy> killed;

    /// <summary>Слово в набрано до конца (для босса — одно из нескольких).</summary>
    public event Action<Enemy, int> wordCompleted;

    private readonly List<string> wordQueue = new List<string>();
    private int wordIndex;
    private string word = string.Empty;
    private char[] typed = new char[0];

    private SpriteRenderer visual;
    private TextMeshPro label;
    private TextMeshPro subLabel;
    private Transform player;
    private float diameter = 0.7f;
    private Color baseColor = new Color(0.85f, 0.35f, 0.3f);

    private bool dying;
    private float deathTimer;
    private float startScale = 1f;

    public string CurrentWord { get { return word; } }
    public int TotalWords { get { return wordQueue.Count; } }
    public int CurrentWordNumber { get { return wordIndex + 1; } }

    // ---------------------------------------------------------------- создание

    public static Enemy Create(Transform parent, Vector3 position, bool boss, float speed,
        List<string> words, TMP_FontAsset font, Transform playerTarget)
    {
        var go = new GameObject(boss ? "Boss" : "Enemy");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        Enemy enemy = go.AddComponent<Enemy>();
        enemy.Build(boss, speed, words, font, playerTarget);
        return enemy;
    }

    private void Build(bool boss, float speed, List<string> words, TMP_FontAsset font, Transform playerTarget)
    {
        IsBoss = boss;
        Speed = speed;
        player = playerTarget;
        Alive = true;

        wordQueue.Clear();
        if (words != null)
            wordQueue.AddRange(words);

        diameter = boss ? 2.2f : 0.7f;
        baseColor = boss ? new Color(0.55f, 0.16f, 0.22f) : PickEnemyColor();

        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = CircleSpriteFactory.Get();
        visual.color = baseColor;
        visual.sortingOrder = boss ? 4 : 2;
        transform.localScale = Vector3.one * diameter;
        startScale = 1f;

        // Размер и отступ надписи не должны зависеть от размера круга:
        // у 3D TMP 10 пунктов ~ 1 юнит, а дети наследуют масштаб родителя, поэтому компенсируем.
        float labelScale = 1f / Mathf.Max(0.01f, diameter);
        float labelLift = (diameter * 0.5f + 0.3f) / Mathf.Max(0.01f, diameter);

        label = CreateLabel("Word", font, boss ? 4.5f : 3.6f,
            new Vector3(0f, labelLift, 0f), boss ? 8 : 6);
        label.transform.localScale = Vector3.one * labelScale;

        if (boss)
        {
            subLabel = CreateLabel("Progress", font, 3f,
                new Vector3(0f, -labelLift, 0f), 8);
            subLabel.transform.localScale = Vector3.one * labelScale;
        }

        HitDistance = 0.45f + diameter * 0.35f;

        LoadWord(0);
    }

    private TextMeshPro CreateLabel(string name, TMP_FontAsset font, float fontSize, Vector3 localPosition, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(6f, 1f);
        tmp.text = string.Empty;
        tmp.color = Color.white;
        tmp.sortingOrder = sortingOrder;
        return tmp;
    }

    private static Color PickEnemyColor()
    {
        // Три оттенка, чтобы враги различались, но читались как «враги»
        int variant = UnityEngine.Random.Range(0, 3);
        switch (variant)
        {
            case 0: return new Color(0.82f, 0.33f, 0.30f);
            case 1: return new Color(0.80f, 0.48f, 0.25f);
            default: return new Color(0.62f, 0.34f, 0.62f);
        }
    }

    // ---------------------------------------------------------------- слово и набор

    private void LoadWord(int index)
    {
        wordIndex = Mathf.Clamp(index, 0, Mathf.Max(0, wordQueue.Count - 1));

        if (wordQueue.Count == 0)
        {
            word = string.Empty;
            typed = new char[0];
        }
        else
        {
            word = wordQueue[wordIndex];
            typed = new char[word.Length];
        }

        if (subLabel != null)
            subLabel.text = "слово " + (wordIndex + 1) + "/" + wordQueue.Count;

        RenderLabel();
    }

    /// <summary>Слово ещё не начали вводить — по нему можно «взять» врага в работу.</summary>
    public bool IsUntouched
    {
        get
        {
            if (!Alive || word.Length == 0)
                return false;

            return typed[0] == '\0';
        }
    }

    /// <summary>Символ, который ждём от игрока сейчас.</summary>
    public char Expected
    {
        get
        {
            for (int i = 0; i < word.Length; i++)
                if (typed[i] == '\0')
                    return word[i];

            return '\0';
        }
    }

    /// <summary>Можно ли выбрать этого врага, нажав символ c.</summary>
    public bool CanBeSelected(char c)
    {
        if (!Alive || IsUntouched == false)
            return false;

        char expected = Expected;
        return expected != '\0' && char.ToUpperInvariant(c) == char.ToUpperInvariant(expected);
    }

    public void SetHighlight(bool on)
    {
        if (Highlighted == on)
            return;

        Highlighted = on;

        if (visual != null)
            visual.color = on ? Color.Lerp(baseColor, Color.white, 0.35f) : baseColor;

        RenderLabel();
    }

    /// <summary>Обработка нажатого символа, когда этот враг уже выбран целью.</summary>
    public EnemyStep Process(char c, bool resetOnMistake)
    {
        if (!Alive || word.Length == 0)
            return EnemyStep.Nothing;

        int position = -1;
        for (int i = 0; i < word.Length; i++)
        {
            if (typed[i] == '\0')
            {
                position = i;
                break;
            }
        }

        if (position < 0)
            return EnemyStep.Nothing;

        char expected = word[position];
        bool correct = char.ToUpperInvariant(c) == char.ToUpperInvariant(expected);

        if (correct)
        {
            typed[position] = expected;
            RenderLabel();

            if (IsWordDone(resetOnMistake))
                return FinishWord();

            return EnemyStep.Correct;
        }

        // Ошибка: показываем неверный символ на позиции
        if (!char.IsControl(c))
            typed[position] = c;

        RenderLabel();

        if (resetOnMistake)
            ResetProgress();
        else if (IsWordDone(false))
            return FinishWord();

        return EnemyStep.Wrong;
    }

    public void ResetProgress()
    {
        Array.Clear(typed, 0, typed.Length);
        RenderLabel();
    }

    /// <summary>
    /// requirePerfect = true: слово считается набранным, когда все буквы верны
    /// (строгий режим, ошибка сбрасывает прогресс).
    /// requirePerfect = false: достаточно заполнить все позиции, ошибки остаются в тексте.
    /// </summary>
    private bool IsWordDone(bool requirePerfect)
    {
        if (word.Length == 0)
            return false;

        for (int i = 0; i < word.Length; i++)
        {
            if (typed[i] == '\0')
                return false;

            if (requirePerfect && typed[i] != word[i])
                return false;
        }

        return true;
    }

    private EnemyStep FinishWord()
    {
        wordCompleted?.Invoke(this, LevelRules.ScoreForWord(word.Length, IsBoss));

        if (IsBoss && wordIndex < wordQueue.Count - 1)
        {
            LoadWord(wordIndex + 1);
            return EnemyStep.WordFinished;
        }

        StartDeath();
        return EnemyStep.EnemyKilled;
    }

    private void StartDeath()
    {
        dying = true;
        deathTimer = 0f;
        Alive = false;
        SetHighlight(false);
    }

    // ---------------------------------------------------------------- движение

    private void Update()
    {
        if (dying)
        {
            deathTimer += Time.deltaTime;
            float t = Mathf.Clamp01(deathTimer / 0.18f);
            transform.localScale = Vector3.one * diameter * Mathf.Lerp(1f, 0f, t);

            if (t >= 1f)
            {
                killed?.Invoke(this);
                Destroy(gameObject);
            }

            return;
        }

        if (!Alive || Frozen || player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.z = 0f;

        if (direction.sqrMagnitude <= HitDistance * HitDistance)
        {
            reachedPlayer?.Invoke(this);
            return;
        }

        transform.position += direction.normalized * (Speed * Time.deltaTime);
    }

    /// <summary>Убрать врага без анимации (например, при рестарте уровня).</summary>
    public void Despawn()
    {
        Alive = false;
        dying = false;
        Destroy(gameObject);
    }

    // ---------------------------------------------------------------- отрисовка текста

    private void RenderLabel()
    {
        if (label == null)
            return;

        if (string.IsNullOrEmpty(word))
        {
            label.text = string.Empty;
            return;
        }

        string pendingHex = "E8ECF5";
        string correctHex = "59E673";
        string errorHex = "FF5348";
        string cursorHex = "FFD140";

        var sb = new System.Text.StringBuilder(word.Length + 24);
        bool cursorHere = Highlighted;

        // Курсор — только на первой ещё не набранной позиции
        int cursorIndex = -1;
        for (int i = 0; i < word.Length; i++)
        {
            if (typed[i] == '\0')
            {
                cursorIndex = i;
                break;
            }
        }

        for (int i = 0; i < word.Length; i++)
        {
            char expected = word[i];
            char current = typed[i];
            bool wrong = current != '\0' && current != expected;
            bool cursor = cursorHere && i == cursorIndex;

            string hex = cursor ? cursorHex : (current == '\0' ? pendingHex : (wrong ? errorHex : correctHex));
            string body = Escape(current == '\0' ? expected : current);

            if (cursor)
                sb.Append("<b><color=#").Append(hex).Append("><u>").Append(body).Append("</u></color></b>");
            else if (wrong)
                sb.Append("<u><color=#").Append(hex).Append(">").Append(body).Append("</color></u>");
            else
                sb.Append("<color=#").Append(hex).Append(">").Append(body).Append("</color>");
        }

        label.text = sb.ToString();
    }

    private static string Escape(char c)
    {
        switch (c)
        {
            case '<': return "&lt;";
            case '>': return "&gt;";
            case '&': return "&amp;";
            default: return c.ToString();
        }
    }
}

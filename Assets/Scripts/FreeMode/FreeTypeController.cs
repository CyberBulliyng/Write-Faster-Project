using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WriteFaster.Samples;

/// <summary>
/// Свободный режим печати: показывает текст строками, проверяет каждый символ,
/// поднимает текст вверх по мере ввода и считает скорость/точность/ошибки.
/// </summary>
public class FreeTypeController : MonoBehaviour
{
    [Header("Строки текста (сверху вниз)")]
    public TMP_Text[] lineViews = new TMP_Text[0];

    [Header("Подписи")]
    public TMP_Text statusText;
    public TMP_Text cpmText;
    public TMP_Text netCpmText;
    public TMP_Text wpmText;
    public TMP_Text netWpmText;
    public TMP_Text accuracyText;
    public TMP_Text errorsText;
    public TMP_Text timeText;
    public TMP_Text progressText;

    [Header("Кнопки (подписываются сами)")]
    public Button restartButton;
    public Button shortTextButton;
    public Button mediumTextButton;
    public Button largeTextButton;
    public Button menuButton;

    [Tooltip("Пока курсор стоит в этом поле, набор текста игры игнорируется (поле своего текста)")]
    public TMP_InputField typingBlockedBy;

    [Header("Цвет")]
    public Color pendingColor = new Color(1f, 1f, 1f, 0.65f);
    public Color correctColor = new Color(0.35f, 0.9f, 0.45f);
    public Color errorColor = new Color(0.95f, 0.32f, 0.32f);
    public Color cursorColor = new Color(1f, 0.82f, 0.25f);

    [Header("Настройки")]
    [Tooltip("На какой строке сверху держать курсор: как только текст доходит до неё, строки поехали вверх")]
    [Range(0, 6)]
    public int cursorRow = 3;

    [Tooltip("Символов в слове для WPM (классика — 5)")]
    public float charsPerWord = 5f;

    [Tooltip("Как часто обновлять показатели, сек")]
    public float hudInterval = 0.15f;

    [Tooltip("Длина строки, если не удалось измерить шрифт")]
    public int fallbackMaxCharsPerLine = 55;

    /// <summary>Вызывается, когда заход завершён (для окна статистики и будущего UI).</summary>
    public event Action<TypingSessionResult> sessionCompleted;

    private readonly TypingTextModel model = new TypingTextModel();
    private string currentText = string.Empty;
    private string currentSampleKey = "medium";

    private bool started;
    private bool finished;
    private double startedAt;
    private double finishedAt;

    private int firstVisibleLine;
    private float hudTimer;

    public TypingTextModel Model { get { return model; } }
    public string CurrentText { get { return currentText; } }
    public bool IsFinished { get { return finished; } }

    // ---------------------------------------------------------------- жизненный цикл

    private void Start()
    {
        if (lineViews == null || lineViews.Length == 0)
        {
            Debug.LogError("FreeTypeController: не назначены lineViews — печатать будет некуда.");
            enabled = false;
            return;
        }

        if (CheckEnterkeyboard.instance == null)
        {
            Debug.LogError("FreeTypeController: на сцене нет CheckEnterkeyboard (объект Manager).");
            return;
        }

        CheckEnterkeyboard.instance.enterNewCharacter += HandleCharacter;
        model.completed += OnModelCompleted;

        WireButtons();
        ApplyText(GetSample(currentSampleKey));
    }

    private void OnDestroy()
    {
        if (CheckEnterkeyboard.instance != null)
            CheckEnterkeyboard.instance.enterNewCharacter -= HandleCharacter;
    }

    private void Update()
    {
        hudTimer += Time.unscaledDeltaTime;
        if (hudTimer < hudInterval)
            return;

        hudTimer = 0f;
        UpdateHud();
    }

    // ---------------------------------------------------------------- кнопки

    private void WireButtons()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(delegate { Restart(); DeselectUi(); });

        if (shortTextButton != null)
            shortTextButton.onClick.AddListener(delegate { LoadSample("short"); DeselectUi(); });

        if (mediumTextButton != null)
            mediumTextButton.onClick.AddListener(delegate { LoadSample("medium"); DeselectUi(); });

        if (largeTextButton != null)
            largeTextButton.onClick.AddListener(delegate { LoadSample("large"); DeselectUi(); });

        if (menuButton != null)
            menuButton.onClick.AddListener(OpenMenu);
    }

    /// <summary>Снимаем выделение с кнопки, иначе Enter «нажимает» её вместо перевода строки.</summary>
    private static void DeselectUi()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void OpenMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name == "Menu")
            return;

        if (Application.CanStreamedLevelBeLoaded("Menu"))
            SceneManager.LoadScene("Menu");
        else
            Debug.LogWarning("Сцена Menu не добавлена в Build Settings — открыть её нельзя.");
    }

    // ---------------------------------------------------------------- текст

    /// <summary>Применить настройки из GameSettings (размер шрифта строк).</summary>
    public void ApplySettings()
    {
        int size = GameSettings.FontSize;
        for (int i = 0; i < lineViews.Length; i++)
        {
            if (lineViews[i] != null && !Mathf.Approximately(lineViews[i].fontSize, size))
                lineViews[i].fontSize = size;
        }
    }

    /// <summary>Поставить новый текст и начать заново.</summary>
    public void ApplyText(string rawText)
    {
        currentText = Sanitize(rawText);

        ApplySettings();

        int maxChars = MeasureMaxCharsPerLine(currentText);
        model.Load(currentText, maxChars, GameSettings.RequireParagraphEnter);

        started = false;
        finished = false;
        startedAt = 0;
        finishedAt = 0;
        firstVisibleLine = 0;
        hudTimer = 1f;

        SetStatus("Печатайте текст. Таймер запустится с первой нажатой клавиши.");
        RenderWindow();
        UpdateHud();
    }

    /// <summary>Тот же текст, но с чистого листа.</summary>
    public void Restart()
    {
        ApplyText(currentText);
    }

    /// <summary>Загрузить встроенный текст: short / medium / large.</summary>
    public void LoadSample(string key)
    {
        currentSampleKey = key;
        ApplyText(GetSample(key));
    }

    private static string GetSample(string key)
    {
        switch (key)
        {
            case "short": return SampleTexts.Short;
            case "large": return SampleTexts.Large;
            default: return SampleTexts.Medium;
        }
    }

    private static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace('\t', ' ')
            .Trim();
    }

    /// <summary>
    /// Подбираем длину строки по ширине области: измеряем реальную ширину отрезка текста
    /// и считаем, сколько таких символов влезает.
    /// </summary>
    private int MeasureMaxCharsPerLine(string text)
    {
        TMP_Text probe = lineViews[0];
        if (probe == null || string.IsNullOrEmpty(text))
            return fallbackMaxCharsPerLine;

        float areaWidth = probe.rectTransform.rect.width;
        if (areaWidth < 100f)
            return fallbackMaxCharsPerLine;

        // Первый абзац, не длиннее 60 символов
        int newline = text.IndexOf('\n');
        string sample = newline >= 0 ? text.Substring(0, newline) : text;
        sample = sample.TrimStart();
        int measuredChars = Math.Min(sample.Length, 60);
        if (measuredChars < 10)
            measuredChars = Math.Min(sample.Length, 10);
        if (measuredChars <= 0)
            return fallbackMaxCharsPerLine;

        sample = sample.Substring(0, measuredChars);

        TextWrappingModes previousWrap = probe.textWrappingMode;
        probe.text = string.Empty;
        probe.textWrappingMode = TextWrappingModes.NoWrap;
        Vector2 preferred = probe.GetPreferredValues(Escape(sample));
        probe.textWrappingMode = previousWrap;

        if (preferred.x <= 1f)
            return fallbackMaxCharsPerLine;

        float perChar = preferred.x / measuredChars;
        int chars = Mathf.FloorToInt(areaWidth / perChar * 0.97f);
        return Mathf.Clamp(chars, 20, 120);
    }

    // ---------------------------------------------------------------- ввод

    /// <summary>Публичный вход для символа с клавиатуры (и для тестов из редактора).</summary>
    public void HandleCharacter(char c)
    {
        if (finished)
            return;

        // Игрок набирает свой текст в поле — игре символы не отдаём.
        // isFocused в этом TMP проставляется только от настоящего клика,
        // поэтому смотрим ещё и на текущий выбор EventSystem.
        if (IsTypingBlocked())
            return;

        if (!started && c != TypingTextModel.BackspaceChar)
        {
            started = true;
            startedAt = Time.realtimeSinceStartupAsDouble;
            SetStatus("Идёт набор. Enter — конец абзаца, Backspace — стереть последний символ.");
        }

        int previousLine = model.Line;
        model.Process(c);

        RenderLine(previousLine);
        RenderLine(model.Line);
        UpdateScroll();
        UpdateHud();
    }

    private bool IsTypingBlocked()
    {
        if (typingBlockedBy == null)
            return false;

        if (typingBlockedBy.isFocused)
            return true;

        return EventSystem.current != null &&
               EventSystem.current.currentSelectedGameObject == typingBlockedBy.gameObject;
    }

    private void OnModelCompleted()
    {
        finished = true;
        finishedAt = Time.realtimeSinceStartupAsDouble;

        SetStatus("<b>Готово!</b>   Символов/мин: " + Cpm.ToString("0") +
                 "   Чистых символов/мин: " + NetCpm.ToString("0") +
                 "   Слов/мин: " + Wpm.ToString("0") +
                 "   Точность: " + (Accuracy * 100f).ToString("0.0") + "%" +
                 "   Ошибки: " + model.ErrorKeystrokes +
                 "   Время: " + ElapsedSeconds.ToString("0.0") + " с");

        TypingSessionResult result = BuildSessionResult();
        StatsStore.AddSession(result);
        sessionCompleted?.Invoke(result);
    }

    // ---------------------------------------------------------------- показатели

    public double ElapsedSeconds
    {
        get
        {
            if (!started)
                return 0;

            double end = finished ? finishedAt : Time.realtimeSinceStartupAsDouble;
            return Math.Max(0.0, end - startedAt);
        }
    }

    // Для скорости время минимум секунда: иначе после первой буквы CPM прыгает в тысячи
    private double TimedSeconds
    {
        get { return Math.Max(1.0, ElapsedSeconds); }
    }

    public double Cpm
    {
        get
        {
            if (!started)
                return 0;
            return (model.CorrectKeystrokes + model.ErrorKeystrokes) / (TimedSeconds / 60.0);
        }
    }

    /// <summary>Чистая скорость: правильные символы минус ошибки за минуту.</summary>
    public double NetCpm
    {
        get
        {
            if (!started)
                return 0;
            double net = (model.CorrectKeystrokes - model.ErrorKeystrokes) / (TimedSeconds / 60.0);
            return net < 0 ? 0 : net;
        }
    }

    public double Wpm { get { return Cpm / Mathf.Max(1f, charsPerWord); } }
    public double NetWpm { get { return NetCpm / Mathf.Max(1f, charsPerWord); } }
    public double Accuracy { get { return model.Accuracy; } }

    private void UpdateHud()
    {
        SetText(cpmText, Cpm.ToString("0"));
        SetText(netCpmText, NetCpm.ToString("0"));
        SetText(wpmText, Wpm.ToString("0"));
        SetText(netWpmText, NetWpm.ToString("0"));
        SetText(accuracyText, (Accuracy * 100f).ToString("0.0") + "%");
        SetText(errorsText, model.ErrorKeystrokes.ToString());
        SetText(timeText, ElapsedSeconds.ToString("0.0") + " c");
        SetText(progressText, (model.Progress * 100f).ToString("0") + "%");
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    private void SetStatus(string message)
    {
        SetText(statusText, message);
    }

    private TypingSessionResult BuildSessionResult()
    {
        var result = new TypingSessionResult
        {
            mode = "free",
            dateIso = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            seconds = ElapsedSeconds,
            correctKeystrokes = model.CorrectKeystrokes,
            errorKeystrokes = model.ErrorKeystrokes,
            textLength = model.TotalPositions,
            cpm = Cpm,
            netCpm = NetCpm,
            wpm = Wpm,
            netWpm = NetWpm,
            accuracy = Accuracy
        };

        foreach (System.Collections.Generic.KeyValuePair<char, int> pair in model.AttemptsByKey)
        {
            int errors = 0;
            model.ErrorsByKey.TryGetValue(pair.Key, out errors);

            result.keys.Add(new KeyStat
            {
                key = pair.Key.ToString(),
                attempts = pair.Value,
                errors = errors
            });
        }

        return result;
    }

    // ---------------------------------------------------------------- отрисовка

    private void UpdateScroll()
    {
        int rows = lineViews.Length;
        if (rows == 0)
            return;

        int cursorLine = Math.Min(model.Line, Math.Max(0, model.Lines.Count - 1));

        // Текст поднимается: курсор всегда стоит на заданной строке,
        // а строки выше уходят за экран (Backspace тоже возвращает окно назад).
        int target = Math.Max(0, cursorLine - cursorRow);
        if (target >= rows)
            target = rows - 1;

        if (target == firstVisibleLine)
            return;

        firstVisibleLine = target;
        RenderWindow();
    }

    private void RenderWindow()
    {
        for (int row = 0; row < lineViews.Length; row++)
            RenderLine(firstVisibleLine + row);
    }

    /// <summary>Перерисовать одну строку, если она попадает в видимое окно.</summary>
    private void RenderLine(int lineIndex)
    {
        int row = lineIndex - firstVisibleLine;
        if (row < 0 || row >= lineViews.Length)
            return;

        TMP_Text view = lineViews[row];
        if (view == null)
            return;

        view.text = lineIndex < model.Lines.Count ? BuildLineRichText(lineIndex) : string.Empty;
    }

    private string BuildLineRichText(int lineIndex)
    {
        TypingLine line = model.Lines[lineIndex];
        if (string.IsNullOrEmpty(line.text))
            return string.Empty;

        bool cursorLine = lineIndex == model.Line && !finished;

        string pendingHex = ColorUtility.ToHtmlStringRGB(pendingColor);
        string correctHex = ColorUtility.ToHtmlStringRGB(correctColor);
        string errorHex = ColorUtility.ToHtmlStringRGB(errorColor);
        string cursorHex = ColorUtility.ToHtmlStringRGB(cursorColor);

        var sb = new StringBuilder(line.text.Length + 32);

        for (int i = 0; i < line.text.Length; i++)
        {
            char expected = line.text[i];
            char typed = line.typed[i];
            bool isCursor = cursorLine && i == model.Col;
            bool wrong = typed != '\0' && typed != expected;

            string hex;
            if (isCursor)
                hex = wrong ? errorHex : cursorHex;
            else if (typed == '\0')
                hex = pendingHex;
            else if (wrong)
                hex = errorHex;
            else
                hex = correctHex;

            char display = typed == '\0' ? expected : typed;
            string body = Escape(display);

            if (isCursor)
            {
                // Подсветка позиции, куда смотрит игрок (mark рисует рамку даже вокруг пробела)
                sb.Append("<mark=#").Append(cursorHex).Append("><b><color=#").Append(hex).Append(">")
                  .Append(body).Append("</color></b></mark>");
            }
            else if (wrong)
            {
                sb.Append("<u><color=#").Append(hex).Append(">").Append(body).Append("</color></u>");
            }
            else
            {
                sb.Append("<color=#").Append(hex).Append(">").Append(body).Append("</color>");
            }
        }

        // Курсор на позиции «нужен Enter» — подсвечиваем блок после последней буквы
        if (cursorLine && model.Col >= line.text.Length && line.requiresEnter && !line.enterTyped)
            sb.Append("<mark=#").Append(cursorHex).Append("> </mark>");

        return sb.ToString();
    }

    /// <summary>Экранируем символы, которые TMP понял бы как разметку.</summary>
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

    private static string Escape(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}

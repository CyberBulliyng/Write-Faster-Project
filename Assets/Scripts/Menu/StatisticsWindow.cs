using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Окно статистики: средние показатели, личные рекорды, топ проблемных клавиш
/// и тепловая карта клавиатуры по ошибкам.
/// Данные берутся из StatsStore (typing_stats.json).
/// </summary>
public class StatisticsWindow : MonoBehaviour
{
    [Header("Кнопка закрытия (подписывается сама)")]
    public Button closeButton;
    public MenuController menu;

    [Header("Текстовые блоки")]
    public TMP_Text summaryText;
    public TMP_Text recordsText;
    public TMP_Text topKeysText;
    public TMP_Text emptyNotice;

    [Header("Тепловая карта")]
    public TMP_Text legendText;
    public Transform rowsParent;
    public GameObject keyCellTemplate;

    [Header("Цвета тепловой карты")]
    public Color unusedKeyColor = new Color(0.22f, 0.24f, 0.30f, 1f);
    public Color noErrorColor = new Color(0.20f, 0.48f, 0.28f, 1f);
    public Color errorColor = new Color(0.78f, 0.25f, 0.22f, 1f);

    [Header("Размер ячейки")]
    public Vector2 cellSize = new Vector2(62f, 46f);
    public Vector2 wideCellSize = new Vector2(150f, 46f);

    // Клавиатура в русской раскладке: буквы + то, что часто встречается в текстах
    private readonly string[] keyboardRows =
    {
        "ЙЦУКЕНГШЩЗХЪ",
        "ФЫВАПРОЛДЖЭ",
        "ЯЧСМИТЬБЮ,.",
        ":-;!?"
    };

    private const string SpaceKey = " ";
    private const string EnterKey = "\n";

    // Awake, а не Start: окно включают из кода (SetActive), и слушатели должны
    // появиться в тот же момент, а не в следующем кадре.
    private void Awake()
    {
        if (closeButton != null)
        {
            if (menu != null)
                closeButton.onClick.AddListener(menu.CloseAllWindows);
            else
                closeButton.onClick.AddListener(delegate { gameObject.SetActive(false); });
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        int sessions = StatsStore.SessionCount;

        double cpm, netCpm, accuracy;
        int errors;
        StatsStore.GetAverages(out cpm, out netCpm, out accuracy, out errors);

        double bestCpm, bestAccuracy;
        StatsStore.GetRecords(out bestCpm, out bestAccuracy);

        bool hasData = sessions > 0;

        if (emptyNotice != null)
            emptyNotice.text = hasData ? string.Empty : "Пока нет ни одной завершённой сессии — сначала попечатай.";

        if (summaryText != null)
        {
            summaryText.text =
                "<size=22><b>Средние за всё время</b></size>\n" +
                "Сессий: " + sessions + "\n" +
                "Символов/мин: " + cpm.ToString("0") + "\n" +
                "Чистых символов/мин: " + netCpm.ToString("0") + "\n" +
                "Слов/мин: " + (cpm / 5f).ToString("0.0") + "\n" +
                "Точность: " + (accuracy * 100f).ToString("0.0") + "%\n" +
                "Всего ошибок: " + errors;
        }

        if (recordsText != null)
        {
            recordsText.text =
                "<size=22><b>Личные рекорды</b></size>\n" +
                "Лучшая скорость: " + bestCpm.ToString("0") + " симв/мин\n" +
                "Лучшая точность: " + (bestAccuracy * 100f).ToString("0.0") + "%";
        }

        if (topKeysText != null)
            topKeysText.text = BuildTopKeysText();

        BuildHeatmap();

        if (legendText != null)
            legendText.text = "Тепловая карта: <color=#6F6F6F>серый</color> — клавиша не встречалась, " +
                              "<color=#33B34A>зелёный</color> — ошибок нет, " +
                              "<color=#FF6B60>красный</color> — ошибки чаще всего. " +
                              "Внизу ячейки — число ошибок.";
    }

    private string BuildTopKeysText()
    {
        List<KeyStat> problems = StatsStore.GetProblemKeys(8);

        var sb = new System.Text.StringBuilder();
        sb.Append("<size=22><b>Топ проблемных клавиш</b></size>\n");

        if (problems.Count == 0)
        {
            sb.Append("Данных пока нет.");
            return sb.ToString();
        }

        bool anyErrors = false;
        for (int i = 0; i < problems.Count; i++)
        {
            KeyStat key = problems[i];
            if (key.errors <= 0)
                continue;

            anyErrors = true;
            float rate = key.attempts > 0 ? (float)key.errors / key.attempts : 0f;
            sb.Append(KeyLabel(key.key) + " — " + key.errors + " ошибок из " + key.attempts +
                      " (" + (rate * 100f).ToString("0") + "%)\n");

            if (sb.Length > 900)
                break;
        }

        if (!anyErrors)
            sb.Append("Ошибок на клавишах пока нет.");

        return sb.ToString();
    }

    private void BuildHeatmap()
    {
        if (rowsParent == null || keyCellTemplate == null)
            return;

        // Очистить старые ряды
        for (int i = rowsParent.childCount - 1; i >= 0; i--)
            Destroy(rowsParent.GetChild(i).gameObject);

        var lookup = new Dictionary<string, KeyStat>();
        foreach (KeyStat stat in StatsStore.TotalKeys)
            lookup[stat.key] = stat;

        // Ряды клавиатуры: три ряда букв, ряд знаков и ряд с Пробелом/Enter
        var rows = new List<List<string>>();
        for (int r = 0; r < keyboardRows.Length; r++)
        {
            var row = new List<string>();
            for (int i = 0; i < keyboardRows[r].Length; i++)
                row.Add(keyboardRows[r][i].ToString());
            rows.Add(row);
        }

        var special = new List<string>();
        special.Add(SpaceKey);
        special.Add(EnterKey);
        rows.Add(special);

        const float cellSpacing = 3f;
        const float rowHeight = 50f;
        const float rowSpacing = 4f;

        for (int r = 0; r < rows.Count; r++)
        {
            List<string> keys = rows[r];

            // Ширина ряда = сумма ширин клавиш + промежутки
            float rowWidth = 0f;
            for (int i = 0; i < keys.Count; i++)
            {
                rowWidth += IsWideKey(keys[i]) ? wideCellSize.x : cellSize.x;
                if (i < keys.Count - 1)
                    rowWidth += cellSpacing;
            }

            var rowGO = new GameObject("Row" + r, typeof(RectTransform));
            var rowRect = rowGO.transform as RectTransform;
            rowRect.SetParent(rowsParent, false);
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -r * (rowHeight + rowSpacing));
            rowRect.sizeDelta = new Vector2(rowWidth, rowHeight);

            // Идём слева направо, центр каждой клавиши — на своей позиции
            float cursor = -rowWidth * 0.5f;

            for (int i = 0; i < keys.Count; i++)
            {
                Vector2 size = IsWideKey(keys[i]) ? wideCellSize : cellSize;
                float centerX = cursor + size.x * 0.5f;

                CreateCell(keys[i], lookup, rowGO.transform, new Vector2(centerX, 0f), size);

                cursor += size.x + cellSpacing;
            }
        }
    }

    private static bool IsWideKey(string key)
    {
        return key == SpaceKey || key == EnterKey;
    }

    private void CreateCell(string key, Dictionary<string, KeyStat> lookup, Transform parent,
        Vector2 anchoredPosition, Vector2 size)
    {
        GameObject cell = Instantiate(keyCellTemplate, parent, false);
        cell.SetActive(true);

        var rect = cell.transform as RectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        KeyStat stat;
        lookup.TryGetValue(KeyStatKey(key), out stat);

        int attempts = stat != null ? stat.attempts : 0;
        int errors = stat != null ? stat.errors : 0;
        float rate = attempts > 0 ? (float)errors / attempts : 0f;

        Color background;
        if (attempts == 0)
            background = unusedKeyColor;
        else
            background = Color.Lerp(noErrorColor, errorColor, Mathf.Clamp01(rate * 1.8f));

        Image image = cell.GetComponent<Image>();
        if (image != null)
            image.color = background;

        TMP_Text[] texts = cell.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text.name == "Count")
            {
                text.text = attempts == 0 ? string.Empty : errors.ToString();
                text.color = attempts == 0 ? new Color(1f, 1f, 1f, 0.25f) : Color.white;
            }
            else
            {
                text.text = KeyLabel(key);
                text.color = Color.white;
            }
        }
    }

    /// <summary>Как клавиша лежит в словаре статистики (буквы — заглавные).</summary>
    private static string KeyStatKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return key;

        char first = key[0];
        return char.IsLetter(first) ? char.ToUpperInvariant(first).ToString() : key;
    }

    /// <summary>Как показать клавишу игроку.</summary>
    private static string KeyLabel(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "?";

        if (key == SpaceKey)
            return "Пробел";

        if (key == EnterKey)
            return "Enter";

        return key;
    }
}

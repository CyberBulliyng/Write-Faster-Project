using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Строит панель паузы (затемнение + панель с кнопками) и вешает компонент PauseMenu
/// на указанный объект-хост. Используется сборщиками сцен режимов.
/// </summary>
public static class PausePanelBuilder
{
    private const string OverlayName = "PauseOverlay";

    public static PauseMenu Build(RectTransform canvasRoot, GameObject host, TMP_FontAsset font)
    {
        Transform existing = canvasRoot.Find(OverlayName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var overlayGO = new GameObject(OverlayName, typeof(RectTransform));
        var overlay = overlayGO.transform as RectTransform;
        overlay.SetParent(canvasRoot, false);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.pivot = new Vector2(0.5f, 0.5f);
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;

        // Затемнение фона и блокировка кликов
        Image dim = Image("Dim", overlay, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.62f));
        dim.raycastTarget = true;

        Image panel = Image("Panel", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 460f), new Color(0.10f, 0.11f, 0.15f, 1f));

        Vector2 centerTop = new Vector2(0.5f, 1f);

        Text("Title", panel.transform, "Пауза", 44, Color.white, TextAlignmentOptions.Center,
            centerTop, centerTop, centerTop, new Vector2(0f, -40f), new Vector2(560f, 64f), font);

        Button resume = Button("ResumeButton", panel.transform, "Продолжить",
            new Vector2(0f, -150f), new Vector2(440f, 74f), new Color(0.22f, 0.48f, 0.30f, 1f), font);
        Button restart = Button("RestartButton", panel.transform, "Заново",
            new Vector2(0f, -240f), new Vector2(440f, 74f), new Color(0.24f, 0.34f, 0.48f, 1f), font);
        Button toMenu = Button("MenuButton", panel.transform, "В главное меню",
            new Vector2(0f, -330f), new Vector2(440f, 74f), new Color(0.48f, 0.28f, 0.28f, 1f), font);

        Text("Hint", panel.transform, "Esc — продолжить игру", 18,
            new Color(0.70f, 0.76f, 0.86f, 1f), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 26f), new Vector2(560f, 30f), font);

        PauseMenu menu = host.GetComponent<PauseMenu>();
        if (menu == null)
            menu = host.AddComponent<PauseMenu>();

        menu.panel = overlayGO;
        menu.resumeButton = resume;
        menu.restartButton = restart;
        menu.menuButton = toMenu;

        overlayGO.SetActive(false);
        return menu;
    }

    // ---------------------------------------------------------------- подсобное

    private static Image Image(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.transform as RectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text Text(string name, Transform parent, string text, int fontSize, Color color,
        TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 sizeDelta, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.transform as RectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.text = text;
        tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button Button(string name, Transform parent, string label, Vector2 anchoredPosition,
        Vector2 size, Color color, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.transform as RectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = new Color(1.15f, 1.1f, 0.75f, 1f);
        colors.fadeDuration = 0.05f;
        button.colors = colors;

        TMP_Text labelTmp = Text("Label", go.transform, label, 26, Color.white, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, font);

        var labelRect = labelTmp.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }
}

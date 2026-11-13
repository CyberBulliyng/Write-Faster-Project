using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Собирает главное меню в сцене Menu: кнопки слева, окна статистики и настроек.
/// Запуск: Write Faster/Построить главное меню. Повторный запуск пересобирает с нуля.
/// </summary>
public static class MenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Menu.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string RootName = "MenuUI";

    private static TMP_FontAsset font;

    private static readonly Color PanelColor = new Color(0.10f, 0.11f, 0.15f, 1f);
    private static readonly Color AccentColor = new Color(0.22f, 0.34f, 0.55f, 1f);

    [MenuItem("Write Faster/Построить главное меню")]
    public static void Build()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("Не найден шрифт TMP: " + FontPath);
            return;
        }

        Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Menu")
            scene = EditorSceneManager.OpenScene(ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        Canvas canvas = EnsureCanvas(out EventSystem eventSystem);

        Transform previous = canvas.transform.Find(RootName);
        if (previous != null)
            Object.DestroyImmediate(previous.gameObject);

        var rootGO = new GameObject(RootName, typeof(RectTransform));
        RectTransform root = rootGO.transform as RectTransform;
        root.SetParent(canvas.transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        // Фон
        NewImage("Background", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.06f, 0.07f, 0.10f, 1f));

        // Левая колонка: заголовок и кнопки
        var column = new GameObject("LeftColumn", typeof(RectTransform));
        var columnRect = column.transform as RectTransform;
        columnRect.SetParent(root, false);
        columnRect.anchorMin = columnRect.anchorMax = new Vector2(0f, 0.5f);
        columnRect.pivot = new Vector2(0f, 0.5f);
        columnRect.anchoredPosition = new Vector2(120f, 0f);
        columnRect.sizeDelta = new Vector2(480f, 900f);

        Vector2 centerTop = new Vector2(0.5f, 0.5f);

        NewText("Title", column.transform, "WRITE FASTER", 66, Color.white,
            TextAlignmentOptions.Center, centerTop, centerTop, centerTop,
            new Vector2(0f, 330f), new Vector2(480f, 90f), false);

        NewText("Subtitle", column.transform, "тренажёр скорости печати", 24,
            new Color(0.65f, 0.72f, 0.85f, 1f), TextAlignmentOptions.Center, centerTop, centerTop, centerTop,
            new Vector2(0f, 258f), new Vector2(480f, 36f), false);

        Button game = MakeButton("GameButton", column.transform, "Игра",
            new Vector2(0f, 150f), new Vector2(460f, 84f), AccentColor);
        Button free = MakeButton("FreeButton", column.transform, "Свободный режим",
            new Vector2(0f, 50f), new Vector2(460f, 84f), AccentColor);
        Button education = MakeButton("EducationButton", column.transform, "Обучение",
            new Vector2(0f, -50f), new Vector2(460f, 84f), AccentColor);
        Button statistics = MakeButton("StatisticsButton", column.transform, "Статистика",
            new Vector2(0f, -150f), new Vector2(460f, 84f), new Color(0.24f, 0.44f, 0.36f, 1f));
        Button settings = MakeButton("SettingsButton", column.transform, "Настройки",
            new Vector2(0f, -250f), new Vector2(460f, 84f), new Color(0.42f, 0.36f, 0.24f, 1f));
        Button quit = MakeButton("QuitButton", column.transform, "Выход",
            new Vector2(0f, -350f), new Vector2(460f, 84f), new Color(0.50f, 0.26f, 0.26f, 1f));

        TMP_Text hint = NewText("Hint", column.transform, "Выбери режим.", 20,
            new Color(0.72f, 0.78f, 0.88f, 1f), TextAlignmentOptions.Top,
            centerTop, centerTop, centerTop, new Vector2(0f, -430f), new Vector2(460f, 90f), true);

        // Окна
        StatisticsWindow statsWindow = BuildStatisticsWindow(root);
        SettingsWindow settingsWindow = BuildSettingsWindow(root);

        // Компонент управления меню
        GameObject menuObject = GameObject.Find("Menu");
        if (menuObject == null)
            menuObject = new GameObject("Menu");

        MenuController controller = menuObject.GetComponent<MenuController>();
        if (controller == null)
            controller = menuObject.AddComponent<MenuController>();

        controller.gameButton = game;
        controller.freeButton = free;
        controller.educationButton = education;
        controller.statisticsButton = statistics;
        controller.settingsButton = settings;
        controller.quitButton = quit;
        controller.statisticsWindow = statsWindow.gameObject;
        controller.settingsWindow = settingsWindow.gameObject;
        controller.hint = hint;

        statsWindow.menu = controller;
        settingsWindow.menu = controller;

        EnsureScenesInBuild();

        EditorUtility.SetDirty(menuObject);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = menuObject;
        Debug.Log("MenuSceneBuilder: главное меню собрано и сохранено.");
    }

    // ---------------------------------------------------------------- окна

    private static StatisticsWindow BuildStatisticsWindow(RectTransform root)
    {
        Image panel = NewImage("StatisticsWindow", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(230f, 0f), new Vector2(1240f, 860f), PanelColor);

        Vector2 topCenter = new Vector2(0.5f, 1f);
        Transform panelT = panel.transform;

        TMP_Text title = NewText("Title", panelT, "Статистика", 40, Color.white,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -34f), new Vector2(80f, 56f), false);

        Button close = MakeButton("CloseButton", panelT, "Закрыть", new Vector2(560f, -46f),
            new Vector2(180f, 56f), new Color(0.45f, 0.30f, 0.30f, 1f));

        TMP_Text emptyNotice = NewText("EmptyNotice", panelT, string.Empty, 22,
            new Color(1f, 0.78f, 0.45f, 1f), TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -100f), new Vector2(-80f, 34f), true);

        // При растянутых якорях sizeDelta — это добавка к размеру якорей, поэтому -60 (620-60=560)
        TMP_Text summary = NewText("SummaryText", panelT, "…", 22, Color.white,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -144f), new Vector2(-60f, 250f), true);

        TMP_Text records = NewText("RecordsText", panelT, "…", 22, Color.white,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -394f), new Vector2(-60f, 150f), true);

        TMP_Text topKeys = NewText("TopKeysText", panelT, "…", 22, Color.white,
            TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-40f, -144f), new Vector2(-60f, 380f), true);

        TMP_Text legend = NewText("LegendText", panelT, "…", 17,
            new Color(0.72f, 0.78f, 0.88f, 1f), TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -540f), new Vector2(-80f, 30f), true);

        // Контейнер тепловой карты: StatisticsWindow раскладывает ряды сам,
        // поэтому layout-групп здесь не нужно (они перебивают позиции рядов)
        var rowsGO = new GameObject("HeatmapRows", typeof(RectTransform));
        var rowsRect = rowsGO.transform as RectTransform;
        rowsRect.SetParent(panelT, false);
        rowsRect.anchorMin = new Vector2(0f, 0f);
        rowsRect.anchorMax = new Vector2(1f, 0f);
        rowsRect.pivot = new Vector2(0.5f, 0f);
        rowsRect.anchoredPosition = new Vector2(0f, 12f);
        rowsRect.sizeDelta = new Vector2(-80f, 272f);

        // Шаблон ячейки (выключен, лежит в отдельном контейнере-хранилище)
        GameObject templates = new GameObject("Templates", typeof(RectTransform));
        var templatesRect = templates.transform as RectTransform;
        templatesRect.SetParent(root, false);
        templatesRect.anchorMin = templatesRect.anchorMax = Vector2.zero;
        templatesRect.sizeDelta = Vector2.zero;

        var cell = new GameObject("KeyCellTemplate", typeof(RectTransform));
        var cellRect = cell.transform as RectTransform;
        cellRect.SetParent(templates.transform, false);
        cellRect.sizeDelta = new Vector2(62f, 54f);

        var cellImage = cell.AddComponent<Image>();
        cellImage.color = new Color(0.22f, 0.24f, 0.30f, 1f);
        cellImage.raycastTarget = false;

        TMP_Text cellLabel = NewText("Label", cell.transform, "Й", 21, Color.white,
            TextAlignmentOptions.Center, new Vector2(0f, 0.28f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, false);

        TMP_Text cellCount = NewText("Count", cell.transform, string.Empty, 13,
            new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0.30f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, false);

        cell.SetActive(false);
        templates.SetActive(false);

        // Компонент окна вешаем на панель, а не на шаблон ячейки
        var window = panel.gameObject.AddComponent<StatisticsWindow>();
        window.closeButton = close;
        window.summaryText = summary;
        window.recordsText = records;
        window.topKeysText = topKeys;
        window.emptyNotice = emptyNotice;
        window.legendText = legend;
        window.rowsParent = rowsRect;
        window.keyCellTemplate = cell;

        panel.gameObject.SetActive(false);
        return window;
    }

    private static SettingsWindow BuildSettingsWindow(RectTransform root)
    {
        Image panel = NewImage("SettingsWindow", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(230f, 0f), new Vector2(900f, 620f), PanelColor);

        Transform panelT = panel.transform;

        NewText("Title", panelT, "Настройки", 38, Color.white,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -34f), new Vector2(-400f, 52f), false);

        Button close = MakeButton("CloseButton", panelT, "Закрыть", new Vector2(330f, -46f),
            new Vector2(180f, 56f), new Color(0.45f, 0.30f, 0.30f, 1f));

        // Размер текста
        NewText("FontLabel", panelT, "Размер текста", 24, Color.white,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -130f), new Vector2(-110f, 40f), false);

        TMP_Text fontValue = NewText("FontValue", panelT, "34", 26, new Color(0.55f, 0.9f, 0.65f, 1f),
            TextAlignmentOptions.Center, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-40f, -130f), new Vector2(80f, 40f), false);

        Slider slider = MakeSlider("FontSlider", panelT, new Vector2(-60f, -130f), new Vector2(420f, 32f),
            GameSettings.MinFontSize, GameSettings.MaxFontSize, GameSettings.FontSize);

        // Enter
        NewText("EnterLabel", panelT, "Enter в конце абзаца", 24, Color.white,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -210f), new Vector2(-120f, 40f), false);

        Button enterToggle = MakeButton("EnterToggleButton", panelT, "Переключить", new Vector2(300f, -212f),
            new Vector2(200f, 52f), new Color(0.30f, 0.42f, 0.60f, 1f));

        TMP_Text enterValue = NewText("EnterValue", panelT, "Вкл", 26, new Color(0.4f, 0.9f, 0.5f, 1f),
            TextAlignmentOptions.Center, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-40f, -210f), new Vector2(110f, 40f), false);

        // Сброс статистики
        NewText("ResetLabel", panelT, "Сброс статистики", 24, Color.white,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -300f), new Vector2(-120f, 40f), false);

        Button reset = MakeButton("ResetButton", panelT, "Очистить всё", new Vector2(300f, -302f),
            new Vector2(200f, 52f), new Color(0.55f, 0.28f, 0.28f, 1f));

        Button resetConfirm = MakeButton("ResetConfirmButton", panelT, "Точно очистить", new Vector2(300f, -370f),
            new Vector2(220f, 48f), new Color(0.70f, 0.25f, 0.25f, 1f));

        Button resetCancel = MakeButton("ResetCancelButton", panelT, "Отмена", new Vector2(80f, -370f),
            new Vector2(160f, 48f), new Color(0.35f, 0.40f, 0.50f, 1f));

        TMP_Text resetHint = NewText("ResetHint", panelT, string.Empty, 20,
            new Color(1f, 0.78f, 0.45f, 1f), TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -430f), new Vector2(-80f, 40f), true);

        TMP_Text status = NewText("StatusText", panelT, string.Empty, 20,
            new Color(0.72f, 0.85f, 0.95f, 1f), TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
            new Vector2(40f, 40f), new Vector2(-80f, 60f), true);

        var window = panelT.gameObject.AddComponent<SettingsWindow>();
        window.closeButton = close;
        window.fontSizeSlider = slider;
        window.fontSizeValue = fontValue;
        window.enterToggleButton = enterToggle;
        window.enterValue = enterValue;
        window.resetButton = reset;
        window.resetConfirmButton = resetConfirm;
        window.resetCancelButton = resetCancel;
        window.resetHint = resetHint;
        window.statusText = status;

        panel.gameObject.SetActive(false);
        return window;
    }

    // ---------------------------------------------------------------- конструкторы UI

    private static Slider MakeSlider(string name, Transform parent, Vector2 anchoredPosition, Vector2 size,
        float min, float max, float value)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.transform as RectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var background = go.AddComponent<Image>();
        background.color = new Color(0.18f, 0.20f, 0.26f, 1f);
        background.raycastTarget = true;

        var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        var fillArea = fillAreaGO.transform as RectTransform;
        fillArea.SetParent(go.transform, false);
        fillArea.anchorMin = new Vector2(0f, 0.25f);
        fillArea.anchorMax = new Vector2(1f, 0.75f);
        fillArea.pivot = new Vector2(0.5f, 0.5f);
        fillArea.offsetMin = new Vector2(8f, 0f);
        fillArea.offsetMax = new Vector2(-8f, 0f);

        var fillGO = new GameObject("Fill", typeof(RectTransform));
        var fillRect = fillGO.transform as RectTransform;
        fillRect.SetParent(fillAreaGO.transform, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var fillImage = fillGO.AddComponent<Image>();
        fillImage.color = new Color(0.35f, 0.75f, 0.45f, 1f);
        fillImage.raycastTarget = false;

        var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        var handleArea = handleAreaGO.transform as RectTransform;
        handleArea.SetParent(go.transform, false);
        handleArea.anchorMin = new Vector2(0f, 0f);
        handleArea.anchorMax = new Vector2(1f, 1f);
        handleArea.pivot = new Vector2(0.5f, 0.5f);
        handleArea.offsetMin = new Vector2(10f, 0f);
        handleArea.offsetMax = new Vector2(-10f, 0f);

        var handleGO = new GameObject("Handle", typeof(RectTransform));
        var handleRect = handleGO.transform as RectTransform;
        handleRect.SetParent(handleAreaGO.transform, false);
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(20f, 32f);
        handleRect.anchoredPosition = Vector2.zero;

        var handleImage = handleGO.AddComponent<Image>();
        handleImage.color = Color.white;
        handleImage.raycastTarget = true;

        Slider slider = go.AddComponent<Slider>();

        // Публичный API — то, что можно задать без приватных полей
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;

        // FillRect/HandleRect у Slider наружу только для чтения — прописываем через SerializedObject
        var so = new SerializedObject(slider);
        so.FindProperty("m_FillRect").objectReferenceValue = fillRect;
        so.FindProperty("m_HandleRect").objectReferenceValue = handleRect;
        so.FindProperty("m_TargetGraphic").objectReferenceValue = handleImage;
        so.ApplyModifiedPropertiesWithoutUndo();

        slider.value = value;
        EditorUtility.SetDirty(slider);
        return slider;
    }

    private static Canvas EnsureCanvas(out EventSystem eventSystem)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(RectTransform));
            canvas = canvasGO.AddComponent<Canvas>();
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var esGO = new GameObject("EventSystem");
            eventSystem = esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }
        else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        return canvas;
    }

    private static void EnsureScenesInBuild()
    {
        string[] wanted =
        {
            "Assets/Scenes/Menu.unity",
            "Assets/Scenes/EducationMode.unity",
            "Assets/Scenes/FreeMode.unity",
            "Assets/Scenes/GameMode.unity",
            "Assets/Scenes/SampleScene.unity"
        };

        var list = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < wanted.Length; i++)
        {
            if (!System.IO.File.Exists(wanted[i]))
            {
                Debug.LogWarning("Сцена не найдена: " + wanted[i]);
                continue;
            }

            list.Add(new EditorBuildSettingsScene(wanted[i], true));
        }

        EditorBuildSettings.scenes = list.ToArray();
    }

    private static Image NewImage(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.transform as RectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text NewText(string name, Transform parent, string text, int fontSize, Color color,
        TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 sizeDelta, bool wrap)
    {
        var go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.transform as RectTransform;
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
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button MakeButton(string name, Transform parent, string label, Vector2 anchoredPosition,
        Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.transform as RectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = new Color(1.15f, 1.1f, 0.75f, 1f);
        colors.fadeDuration = 0.05f;
        button.colors = colors;

        TMP_Text labelTmp = NewText("Label", go.transform, label, 26, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);

        RectTransform labelRect = labelTmp.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }
}

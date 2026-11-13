using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Собирает UI свободного режима в сцене FreeMode: HUD, строки текста, поле вставки, кнопки.
/// Запуск: меню Write Faster -> Построить UI свободного режима.
/// Можно запускать повторно: старая сборка удаляется и строится заново.
/// </summary>
public static class FreeModeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/FreeMode.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string RootName = "FreeModeUI";

    private const int LineCount = 12;
    private const float LineHeight = 52f;
    private const int LineFontSize = 34;
    private const float PanelHeight = 240f;

    private static TMP_FontAsset font;

    [MenuItem("Write Faster/Построить UI свободного режима")]
    public static void Build()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("Не найден шрифт TMP: " + FontPath);
            return;
        }

        Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "FreeMode")
            scene = EditorSceneManager.OpenScene(ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("В сцене FreeMode нет Canvas.");
            return;
        }

        // 1. Overlay вместо Screen Space - Camera: ортокамера обрезала края канваса
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 2. Если уже строили — убрать старое
        Transform previous = canvas.transform.Find(RootName);
        if (previous != null)
            UnityEngine.Object.DestroyImmediate(previous.gameObject);

        // 3. Тестовые объекты WordChecker спрятать в выключенную папку
        HideLegacyTestObjects(canvas.transform);

        // 4. Корень новой сборки
        var rootGO = new GameObject(RootName, typeof(RectTransform));
        RectTransform root = rootGO.transform as RectTransform;
        root.SetParent(canvas.transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        // 5. Верхняя панель показателей
        Image hudBar = NewImage("HudBar", root,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 100f), new Color(0.10f, 0.11f, 0.15f, 0.95f));

        string[] captions =
        {
            "Символов/мин", "Чистый симв/мин", "Слов/мин", "Чистый слов/мин",
            "Точность", "Ошибки", "Время", "Прогресс"
        };

        var values = new List<TMP_Text>();
        Vector2 topCenter = new Vector2(0.5f, 1f);

        for (int i = 0; i < captions.Length; i++)
        {
            float x = (i - (captions.Length - 1) * 0.5f) * 220f;

            NewText("Caption_" + i, hudBar.transform, captions[i], 17,
                new Color(0.62f, 0.68f, 0.78f, 1f), TextAlignmentOptions.Center,
                topCenter, topCenter, topCenter, new Vector2(x, -16f), new Vector2(220f, 24f));

            values.Add(NewText("Value_" + i, hudBar.transform, "0", 34,
                Color.white, TextAlignmentOptions.Center,
                topCenter, topCenter, topCenter, new Vector2(x, -58f), new Vector2(220f, 42f)));
        }

        TMP_Text statusText = NewText("StatusText", root,
            "Печатайте текст. Таймер стартует с первой нажатой клавиши.", 23,
            new Color(0.82f, 0.88f, 1f, 1f), TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -132f), new Vector2(-80f, 36f));

        // 6. Область текста
        var typingGO = new GameObject("TypingArea", typeof(RectTransform));
        RectTransform typing = typingGO.transform as RectTransform;
        typing.SetParent(root, false);
        typing.anchorMin = new Vector2(0f, 1f);
        typing.anchorMax = new Vector2(1f, 1f);
        typing.pivot = new Vector2(0.5f, 1f);
        typing.anchoredPosition = new Vector2(0f, -172f);
        typing.sizeDelta = new Vector2(-80f, LineCount * LineHeight);

        var lineViews = new List<TMP_Text>();
        for (int i = 0; i < LineCount; i++)
        {
            TMP_Text line = NewText("Line_" + (i + 1), typing, string.Empty, LineFontSize,
                Color.white, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -i * LineHeight), new Vector2(0f, LineHeight));

            line.textWrappingMode = TextWrappingModes.NoWrap;
            lineViews.Add(line);
        }

        // 7. Нижняя панель (y детей считается вверх от её нижнего края)
        Image panel = NewImage("BottomPanel", root,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0f, PanelHeight), new Color(0.10f, 0.11f, 0.15f, 0.95f));

        Vector2 bottomCenter = new Vector2(0.5f, 0f);

        TMP_InputField pasteField = NewInputField("PasteField", panel.transform,
            new Vector2(-400f, 180f), new Vector2(1060f, 100f));

        Button apply = MakeButton("ApplyButton", panel.transform, "Применить",
            new Vector2(250f, 205f), new Vector2(190f, 58f), new Color(0.20f, 0.55f, 0.32f, 1f));

        Button clear = MakeButton("ClearButton", panel.transform, "Очистить",
            new Vector2(250f, 140f), new Vector2(190f, 58f), new Color(0.45f, 0.30f, 0.30f, 1f));

        TMP_Text messageText = NewText("MessageText", panel.transform,
            "Можно вставить свой текст (до 25 000 символов) или открыть файл .txt / .docx.", 20,
            new Color(0.72f, 0.78f, 0.88f, 1f), TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 84f), new Vector2(-80f, 32f));

        Button restart = MakeButton("RestartButton", panel.transform, "Заново",
            new Vector2(-620f, 30f), new Vector2(220f, 56f), new Color(0.35f, 0.40f, 0.55f, 1f));
        Button shortText = MakeButton("ShortTextButton", panel.transform, "Короткий",
            new Vector2(-372f, 30f), new Vector2(220f, 56f), new Color(0.24f, 0.34f, 0.48f, 1f));
        Button mediumText = MakeButton("MediumTextButton", panel.transform, "Средний",
            new Vector2(-124f, 30f), new Vector2(220f, 56f), new Color(0.24f, 0.34f, 0.48f, 1f));
        Button largeText = MakeButton("LargeTextButton", panel.transform, "Длинный",
            new Vector2(124f, 30f), new Vector2(220f, 56f), new Color(0.24f, 0.34f, 0.48f, 1f));
        Button loadFile = MakeButton("LoadFileButton", panel.transform, "Открыть файл",
            new Vector2(372f, 30f), new Vector2(220f, 56f), new Color(0.24f, 0.34f, 0.48f, 1f));
        Button menu = MakeButton("MenuButton", panel.transform, "В меню",
            new Vector2(620f, 30f), new Vector2(220f, 56f), new Color(0.48f, 0.28f, 0.28f, 1f));

        // 8. Компоненты и связи на объекте Manager
        GameObject manager = GameObject.Find("Manager");
        if (manager == null)
            manager = new GameObject("Manager");

        if (manager.GetComponent<CheckEnterkeyboard>() == null)
            manager.AddComponent<CheckEnterkeyboard>();

        WordChecker legacyChecker = manager.GetComponent<WordChecker>();
        if (legacyChecker != null)
            legacyChecker.enabled = false;

        FreeTypeController controller = manager.GetComponent<FreeTypeController>();
        if (controller == null)
            controller = manager.AddComponent<FreeTypeController>();

        controller.lineViews = lineViews.ToArray();
        controller.statusText = statusText;
        controller.cpmText = values[0];
        controller.netCpmText = values[1];
        controller.wpmText = values[2];
        controller.netWpmText = values[3];
        controller.accuracyText = values[4];
        controller.errorsText = values[5];
        controller.timeText = values[6];
        controller.progressText = values[7];
        controller.restartButton = restart;
        controller.shortTextButton = shortText;
        controller.mediumTextButton = mediumText;
        controller.largeTextButton = largeText;
        controller.menuButton = menu;
        controller.typingBlockedBy = pasteField;
        controller.cursorRow = 3;

        CustomTextLoader loader = manager.GetComponent<CustomTextLoader>();
        if (loader == null)
            loader = manager.AddComponent<CustomTextLoader>();

        loader.typeController = controller;
        loader.pasteField = pasteField;
        loader.applyButton = apply;
        loader.loadFileButton = loadFile;
        loader.clearFieldButton = clear;
        loader.messageText = messageText;

        // 9. Сцены в Build Settings
        EnsureScenesInBuild();

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = manager;
        Debug.Log("FreeModeSceneBuilder: UI свободного режима собран и сохранён.");
    }

    // ---------------------------------------------------------------- подсобные функции

    private static void HideLegacyTestObjects(Transform canvasTransform)
    {
        string[] legacyNames = { "Text (TMP)", "Утеук", "1", "2" };
        var found = new List<Transform>();

        for (int i = 0; i < legacyNames.Length; i++)
        {
            Transform t = canvasTransform.Find(legacyNames[i]);
            if (t != null)
                found.Add(t);
        }

        if (found.Count == 0)
            return;

        Transform folder = canvasTransform.Find("TEST_WordChecker");
        if (folder == null)
        {
            var folderGO = new GameObject("TEST_WordChecker", typeof(RectTransform));
            RectTransform rect = folderGO.transform as RectTransform;
            rect.SetParent(canvasTransform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            folder = rect;
        }

        for (int i = 0; i < found.Count; i++)
            found[i].SetParent(folder, false);

        folder.gameObject.SetActive(false);
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

            // В Unity 6 у конструктора параметр — путь к сцене
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
        Vector2 anchoredPosition, Vector2 sizeDelta)
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
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
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
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
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

        TMP_Text labelTmp = NewText("Label", go.transform, label, 21, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        RectTransform labelRect = labelTmp.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private static TMP_InputField NewInputField(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.transform as RectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.color = new Color(0.16f, 0.18f, 0.23f, 1f);
        image.raycastTarget = true;

        var textAreaGO = new GameObject("Text Area", typeof(RectTransform));
        RectTransform textArea = textAreaGO.transform as RectTransform;
        textArea.SetParent(go.transform, false);
        textArea.anchorMin = Vector2.zero;
        textArea.anchorMax = Vector2.one;
        textArea.pivot = new Vector2(0.5f, 0.5f);
        textArea.offsetMin = new Vector2(12f, 8f);
        textArea.offsetMax = new Vector2(-12f, -8f);
        textAreaGO.AddComponent<RectMask2D>();

        TMP_Text placeholder = NewText("Placeholder", textArea,
            "Вставьте сюда свой текст (до 25 000 символов)…", 19,
            new Color(0.6f, 0.6f, 0.6f, 0.75f), TextAlignmentOptions.TopLeft,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        placeholder.fontStyle = FontStyles.Italic;

        TMP_Text text = NewText("Text", textArea, string.Empty, 19, Color.white,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);

        placeholder.textWrappingMode = TextWrappingModes.Normal;
        placeholder.overflowMode = TextOverflowModes.Overflow;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = textArea;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.characterLimit = 25000;
        input.pointSize = 19;
        input.caretColor = Color.white;
        input.selectionColor = new Color(0.3f, 0.5f, 0.9f, 0.6f);
        input.scrollSensitivity = 20f;
        input.text = string.Empty;
        return input;
    }
}

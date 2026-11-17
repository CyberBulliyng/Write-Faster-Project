using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Собирает сцену GameMode: камера, игрок в центре, контейнер врагов,
/// верхний HUD (очки, прогресс, таймер, уровень, жизни), панель паузы и компоненты.
/// Запуск: Write Faster/Построить игровой режим.
/// </summary>
public static class GameModeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/GameMode.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string RootName = "GameUI";

    private static TMP_FontAsset font;

    [MenuItem("Write Faster/Построить игровой режим")]
    public static void Build()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("Не найден шрифт TMP: " + FontPath);
            return;
        }

        Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "GameMode")
            scene = EditorSceneManager.OpenScene(ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        // Камера: ортографическая, тёмный фон вместо неба
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            var camGO = new GameObject("Main Camera");
            camera = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            camGO.tag = "MainCamera";
        }

        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.055f, 0.085f, 1f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;

        Canvas canvas = EnsureCanvas();

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

        // Игрок в центре мира. Спрайт круга создаётся кодом уже в рантайме
        // (в сцене сохранять сгенерированный спрайт нельзя — ссылка обломается).
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            var sprite = player.AddComponent<SpriteRenderer>();
            sprite.color = new Color(0.32f, 0.72f, 0.95f, 1f);
            sprite.sortingOrder = 3;
            player.transform.localScale = Vector3.one * 0.9f;
        }

        // Контейнер для врагов
        GameObject enemies = GameObject.Find("Enemies");
        if (enemies == null)
            enemies = new GameObject("Enemies");

        // Верхняя панель
        Image topBar = NewImage("TopBar", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 110f), new Color(0.09f, 0.10f, 0.14f, 0.96f));

        Vector2 topCenter = new Vector2(0.5f, 1f);

        TMP_Text levelText = NewText("LevelText", topBar.transform, "Уровень 1", 30, Color.white,
            TextAlignmentOptions.Center, topCenter, topCenter, topCenter, new Vector2(-700f, -14f), new Vector2(400f, 44f));

        TMP_Text scoreText = NewText("ScoreText", topBar.transform, "Очки: 0", 22,
            new Color(0.85f, 0.9f, 1f, 1f), TextAlignmentOptions.Center, topCenter, topCenter, topCenter,
            new Vector2(-300f, -14f), new Vector2(300f, 32f));
        Image scoreBar = Bar("ScoreBar", topBar.transform, new Vector2(-300f, -56f), new Vector2(300f, 16f),
            new Color(0.35f, 0.75f, 0.45f, 1f));

        TMP_Text timerText = NewText("TimerText", topBar.transform, "00:00", 28, Color.white,
            TextAlignmentOptions.Center, topCenter, topCenter, topCenter, new Vector2(60f, -14f), new Vector2(200f, 40f));

        TMP_Text progressText = NewText("ProgressText", topBar.transform, "Врагов осталось: 6", 22,
            new Color(0.85f, 0.9f, 1f, 1f), TextAlignmentOptions.Center, topCenter, topCenter, topCenter,
            new Vector2(360f, -14f), new Vector2(340f, 32f));
        Image progressBar = Bar("ProgressBar", topBar.transform, new Vector2(360f, -56f), new Vector2(340f, 16f),
            new Color(0.35f, 0.6f, 0.9f, 1f));

        TMP_Text healthText = NewText("HealthText", topBar.transform, "Жизни: 3/3", 22,
            new Color(1f, 0.8f, 0.8f, 1f), TextAlignmentOptions.Center, topCenter, topCenter, topCenter,
            new Vector2(740f, -14f), new Vector2(300f, 32f));
        Image healthBar = Bar("HealthBar", topBar.transform, new Vector2(740f, -56f), new Vector2(300f, 16f),
            new Color(0.85f, 0.35f, 0.32f, 1f));

        TMP_Text toastText = NewText("ToastText", root, "Уровень 1", 26, new Color(0.95f, 0.97f, 1f, 1f),
            TextAlignmentOptions.Center, topCenter, topCenter, topCenter, new Vector2(0f, -150f), new Vector2(1500f, 44f));

        NewText("BottomHint", root, "Набирай слово врага. Esc — пауза.", 20,
            new Color(0.68f, 0.74f, 0.85f, 1f), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 30f), new Vector2(900f, 32f));

        // Вспышка при уроне (в самом конце, чтобы лесть поверх мира)
        Image damageFlash = NewImage("DamageFlash", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.8f, 0.1f, 0.1f, 0f));
        damageFlash.raycastTarget = false;

        // Менеджер режима
        GameObject manager = GameObject.Find("Manager");
        if (manager == null)
            manager = new GameObject("Manager");

        if (manager.GetComponent<CheckEnterkeyboard>() == null)
            manager.AddComponent<CheckEnterkeyboard>();

        WordChecker legacyChecker = manager.GetComponent<WordChecker>();
        if (legacyChecker != null)
            legacyChecker.enabled = false;

        GameModeController controller = manager.GetComponent<GameModeController>();
        if (controller == null)
            controller = manager.AddComponent<GameModeController>();

        controller.player = player.transform;
        controller.enemyRoot = enemies.transform;
        controller.wordFont = font;
        controller.scoreText = scoreText;
        controller.scoreBar = scoreBar;
        controller.progressText = progressText;
        controller.progressBar = progressBar;
        controller.timerText = timerText;
        controller.levelText = levelText;
        controller.healthText = healthText;
        controller.healthBar = healthBar;
        controller.toastText = toastText;
        controller.damageFlash = damageFlash;

        PausePanelBuilder.Build(root, manager, font);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = manager;
        Debug.Log("GameModeSceneBuilder: игровой режим собран и сохранён.");
    }

    // ---------------------------------------------------------------- подсобное

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
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

        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        return canvas;
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

        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image Bar(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.transform as RectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.18f, 0.20f, 0.26f, 1f);
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        image.raycastTarget = false;

        // Отдельная заливка поверх подложки
        var fillGO = new GameObject("Fill", typeof(RectTransform));
        RectTransform fillRect = fillGO.transform as RectTransform;
        fillRect.SetParent(go.transform, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var fill = fillGO.AddComponent<Image>();
        fill.color = color;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        fill.raycastTarget = false;

        // Возвращаем картинку заливки — контроллер будет менять fillAmount
        return fill;
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
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Меню паузы. Работает в любом режиме: Esc останавливает время,
/// отключает ввод и показывает панель с кнопками.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Панель (скрыта, пока не нажали Esc)")]
    public GameObject panel;

    [Header("Кнопки (подписываются сами)")]
    public Button resumeButton;
    public Button restartButton;
    public Button menuButton;

    /// <summary>Сцена подписывается, чтобы перезапустить режим той же кнопкой.</summary>
    public event System.Action restartRequested;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        // Статика обнуляется при перезагрузке домена — восстанавливаем ссылку
        if (Instance == null)
            Instance = this;
    }

    /// <summary>Пауза текущей сцены, даже если статическая ссылка потерялась.</summary>
    public static PauseMenu Find()
    {
        if (Instance == null)
            Instance = UnityEngine.Object.FindFirstObjectByType<PauseMenu>();

        return Instance;
    }

    private void Start()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(delegate { SetPaused(false); });

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (menuButton != null)
            menuButton.onClick.AddListener(GoToMenu);

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Сцена выгружается — возвращаем время и ввод в нормальное состояние
        Time.timeScale = 1f;
        CheckEnterkeyboard.SetInputEnabled(true);

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;

        Time.timeScale = paused ? 0f : 1f;
        CheckEnterkeyboard.SetInputEnabled(!paused);

        if (panel != null)
            panel.SetActive(paused);

        if (paused)
            FocusResume();
        else
            ClearSelection();
    }

    private void OnRestartClicked()
    {
        SetPaused(false);

        if (restartRequested != null)
            restartRequested.Invoke();
        else
            Debug.Log("PauseMenu: режим не подписался на restartRequested — перезапуск не выполнен.");
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        CheckEnterkeyboard.SetInputEnabled(true);

        if (Application.CanStreamedLevelBeLoaded("Menu"))
            SceneManager.LoadScene("Menu");
        else
            Debug.LogError("PauseMenu: сцена Menu не добавлена в Build Settings.");
    }

    private void FocusResume()
    {
        if (EventSystem.current == null || resumeButton == null)
            return;

        EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    private void ClearSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}

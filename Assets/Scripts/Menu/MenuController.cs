using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Главное меню: шесть кнопок слева, переходы в режимы, окна статистики и настроек, выход.
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Кнопки (подписываются сами)")]
    public Button gameButton;
    public Button freeButton;
    public Button educationButton;
    public Button statisticsButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Окна")]
    public GameObject statisticsWindow;
    public GameObject settingsWindow;

    [Header("Подпись под кнопками")]
    public TMP_Text hint;

    private void Start()
    {
        if (gameButton != null)
            gameButton.onClick.AddListener(delegate { LoadScene("GameMode"); });

        if (freeButton != null)
            freeButton.onClick.AddListener(delegate { LoadScene("FreeMode"); });

        if (educationButton != null)
            educationButton.onClick.AddListener(delegate { LoadScene("EducationMode"); });

        if (statisticsButton != null)
            statisticsButton.onClick.AddListener(delegate { ToggleWindow(statisticsWindow, settingsWindow); });

        if (settingsButton != null)
            settingsButton.onClick.AddListener(delegate { ToggleWindow(settingsWindow, statisticsWindow); });

        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);

        SetHint("Выбери режим. Esc закрывает открытое окно.");
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
            CloseAllWindows();
    }

    private void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SetHint("Сцена " + sceneName + " не добавлена в Build Settings.");
            Debug.LogError("MenuController: сцена " + sceneName + " отсутствует в Build Settings.");
            return;
        }

        SetHint("Загружаю: " + sceneName + "…");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Показать нужное окно, спрятать другое.</summary>
    private void ToggleWindow(GameObject show, GameObject hide)
    {
        if (hide != null)
            hide.SetActive(false);

        if (show == null)
        {
            SetHint("Окно не найдено на сцене.");
            return;
        }

        bool willBeVisible = !show.activeSelf;
        show.SetActive(willBeVisible);

        if (willBeVisible)
        {
            // Окно само обновит данные в OnEnable
            SetHint(show.name + " — Esc или кнопка «Закрыть», чтобы вернуть меню.");
            FocusFirstButton(show);
        }
        else
        {
            SetHint("Выбери режим.");
            ClearSelection();
        }
    }

    public void CloseAllWindows()
    {
        if (statisticsWindow != null)
            statisticsWindow.SetActive(false);

        if (settingsWindow != null)
            settingsWindow.SetActive(false);

        SetHint("Выбери режим.");
        ClearSelection();
    }

    private static void FocusFirstButton(GameObject window)
    {
        // Чтобы стрелки/Tab работали внутри окна, выведем фокус на его первую кнопку
        Button first = window.GetComponentInChildren<Button>();
        if (first != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    private static void ClearSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void Quit()
    {
        SetHint("Выход…");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetHint(string text)
    {
        if (hint != null)
            hint.text = text;
    }
}

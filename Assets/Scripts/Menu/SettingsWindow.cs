using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Окно настроек: размер текста, нужно ли жать Enter в конце абзаца, сброс статистики.
/// Всё сразу пишется в typing_settings.json / typing_stats.json.
/// </summary>
public class SettingsWindow : MonoBehaviour
{
    [Header("Кнопка закрытия")]
    public Button closeButton;
    public MenuController menu;

    [Header("Размер текста")]
    public Slider fontSizeSlider;
    public TMP_Text fontSizeValue;

    [Header("Enter в конце абзаца")]
    public Button enterToggleButton;
    public TMP_Text enterValue;

    [Header("Сброс статистики")]
    public Button resetButton;
    public Button resetConfirmButton;
    public Button resetCancelButton;
    public TMP_Text resetHint;

    [Header("Подпись")]
    public TMP_Text statusText;

    private bool loadingSettings;
    private bool waitingForResetConfirm;

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

        if (fontSizeSlider != null)
        {
            fontSizeSlider.minValue = GameSettings.MinFontSize;
            fontSizeSlider.maxValue = GameSettings.MaxFontSize;
            fontSizeSlider.wholeNumbers = true;
            fontSizeSlider.onValueChanged.AddListener(OnFontSizeChanged);
        }

        if (enterToggleButton != null)
            enterToggleButton.onClick.AddListener(ToggleEnter);

        if (resetButton != null)
            resetButton.onClick.AddListener(AskReset);

        if (resetConfirmButton != null)
        {
            resetConfirmButton.onClick.AddListener(ConfirmReset);
            resetConfirmButton.gameObject.SetActive(false);
        }

        if (resetCancelButton != null)
        {
            resetCancelButton.onClick.AddListener(CancelReset);
            resetCancelButton.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        CancelReset();
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        loadingSettings = true;

        if (fontSizeSlider != null)
            fontSizeSlider.value = GameSettings.FontSize;

        SetFontSizeLabel(GameSettings.FontSize);
        SetEnterLabel(GameSettings.RequireParagraphEnter);

        loadingSettings = false;
    }

    private void OnFontSizeChanged(float value)
    {
        if (loadingSettings)
            return;

        int size = Mathf.RoundToInt(value);
        GameSettings.SetFontSize(size);
        SetFontSizeLabel(size);
        SetStatus("Размер текста: " + size + ". Применится при следующем заходе в режим.");
    }

    private void ToggleEnter()
    {
        bool newValue = !GameSettings.RequireParagraphEnter;
        GameSettings.SetRequireParagraphEnter(newValue);
        SetEnterLabel(newValue);
        SetStatus(newValue
            ? "Enter в конце абзаца нужен."
            : "Enter не нужен — абзац перескакивается автоматически.");
    }

    private void AskReset()
    {
        waitingForResetConfirm = true;

        if (resetConfirmButton != null)
            resetConfirmButton.gameObject.SetActive(true);

        if (resetCancelButton != null)
            resetCancelButton.gameObject.SetActive(true);

        if (resetButton != null)
            resetButton.interactable = false;

        if (resetHint != null)
            resetHint.text = "Удалить всю статистику (" + StatsStore.SessionCount + " сессий)? Это нельзя отменить.";
    }

    private void ConfirmReset()
    {
        StatsStore.ResetAll();
        CancelReset();

        if (resetHint != null)
            resetHint.text = "Статистика очищена.";

        SetStatus("Файл статистики пуст — можно начинать заново.");
    }

    private void CancelReset()
    {
        waitingForResetConfirm = false;

        if (resetConfirmButton != null)
            resetConfirmButton.gameObject.SetActive(false);

        if (resetCancelButton != null)
            resetCancelButton.gameObject.SetActive(false);

        if (resetButton != null)
            resetButton.interactable = true;

        if (resetHint != null && !waitingForResetConfirm)
            resetHint.text = string.Empty;
    }

    private void SetFontSizeLabel(int size)
    {
        if (fontSizeValue != null)
            fontSizeValue.text = size.ToString("0");
    }

    private void SetEnterLabel(bool enabled)
    {
        if (enterValue == null)
            return;

        enterValue.text = enabled ? "Вкл" : "Выкл";
        enterValue.color = enabled ? new Color(0.4f, 0.9f, 0.5f) : new Color(0.85f, 0.6f, 0.35f);
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
}

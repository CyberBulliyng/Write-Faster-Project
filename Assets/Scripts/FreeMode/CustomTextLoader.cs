using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Загрузка своего текста в свободном режиме:
/// 1) вставить в поле и нажать «Применить» (максимум 25000 символов);
/// 2) открыть файл .txt или .docx.
/// Пока игрок печатает в поле, клавиатура игры отключена, чтобы символы не уходили в набор.
/// </summary>
public class CustomTextLoader : MonoBehaviour
{
    [Header("Ссылки")]
    public FreeTypeController typeController;
    public TMP_InputField pasteField;
    public Button applyButton;
    public Button loadFileButton;
    public Button clearFieldButton;
    public TMP_Text messageText;

    [Header("Ограничение")]
    public int maxCharacters = DocumentTextReader.MaxCharacters;

    [Header("Окно выбора файла (Windows)")]
    public string dialogTitle = "Выберите файл с текстом";
    public string dialogFilter = "Текст (*.txt;*.docx)\0*.txt;*.docx\0Все файлы (*.*)\0*.*\0\0";

    private string lastFolder;

    private void Start()
    {
        if (typeController == null)
            typeController = UnityEngine.Object.FindFirstObjectByType<FreeTypeController>();

        if (typeController == null)
        {
            Debug.LogError("CustomTextLoader: на сцене нет FreeTypeController — загружать текст некуда.");
            return;
        }

        if (pasteField != null)
        {
            pasteField.characterLimit = maxCharacters;
            // Игровой ввод блокируется самой игрой: FreeTypeController смотрит,
            // активен ли focus в поле (typingBlockedBy.isFocused).
        }

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyFromField);

        if (loadFileButton != null)
            loadFileButton.onClick.AddListener(LoadFromFile);

        if (clearFieldButton != null)
            clearFieldButton.onClick.AddListener(ClearField);

        SetMessage("Можно вставить свой текст (до " + maxCharacters + " символов) или открыть файл .txt / .docx.");
    }

    public void ApplyFromField()
    {
        if (pasteField == null)
            return;

        try
        {
            string text = DocumentTextReader.PreparePasted(pasteField.text);
            text = CutToLimit(text);

            typeController.ApplyText(text);
            SetMessage("Свой текст загружен: " + text.Length + " символов.");
        }
        catch (Exception e)
        {
            SetMessage("Ошибка: " + e.Message);
        }

        ReturnFocusToGame();
    }

    public void LoadFromFile()
    {
        string path = WindowsOpenFileDialog.Open(dialogTitle, dialogFilter, lastFolder);

        if (string.IsNullOrEmpty(path))
        {
            SetMessage("Файл не выбран.");
            ReturnFocusToGame();
            return;
        }

        try
        {
            string text = DocumentTextReader.Read(path);
            lastFolder = Path.GetDirectoryName(path);

            if (pasteField != null)
                pasteField.text = text;

            typeController.ApplyText(text);
            SetMessage("Загружено из файла " + Path.GetFileName(path) + ": " + text.Length + " символов.");
        }
        catch (Exception e)
        {
            SetMessage("Не удалось прочитать файл: " + e.Message);
        }

        ReturnFocusToGame();
    }

    public void ClearField()
    {
        if (pasteField != null)
            pasteField.text = string.Empty;

        SetMessage("Поле очищено.");
        ReturnFocusToGame();
    }

    private string CutToLimit(string text)
    {
        if (text.Length <= maxCharacters)
            return text;

        SetMessage("Текст обрезан до " + maxCharacters + " символов.");
        return text.Substring(0, maxCharacters);
    }

    /// <summary>Убираем выделение с поля/кнопки, чтобы Enter и пробел шли в набор текста.</summary>
    private void ReturnFocusToGame()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        CheckEnterkeyboard.SetInputEnabled(true);
    }

    private void SetMessage(string text)
    {
        if (messageText != null)
            messageText.text = text;
    }
}

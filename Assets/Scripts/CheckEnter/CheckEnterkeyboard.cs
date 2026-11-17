using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Единая точка входа клавиатурного ввода.
/// Печатные символы берутся из Keyboard.onTextInput,
/// Enter и Backspace опрашиваются в Update и передаются как '\n' и '\b'.
/// </summary>
public class CheckEnterkeyboard : MonoBehaviour
{
    public static CheckEnterkeyboard instance;

    // Выключается, пока игрок печатает свой текст в поле ввода
    public bool inputEnabled = true;

    public char currentCharacter;

    public event Action<char> enterNewCharacter;

    private void Awake()
    {
        // Источник ввода — свой в каждой сцене. Раньше тут был DontDestroyOnLoad,
        // и вместе с клавиатурой в следующую сцену уезжали контроллеры режима,
        // которые продолжали ловить ввод уже в меню.
        // Если на сцене два объекта ввода — последняя разбуженная забирает управление,
        // дубли просто не получают подписчиков.
        if (instance != null && instance != this && instance.gameObject.scene == gameObject.scene)
            Debug.Log("CheckEnterkeyboard: на сцене два объекта ввода, управляю этот.");

        instance = this;
    }

    private void OnEnable()
    {
        // Статика обнуляется при перезагрузке домена — восстанавливаем ссылку
        if (instance == null)
            instance = this;

        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput += OnTextInput;
        }
    }

    /// <summary>
    /// Текущий источник ввода. Если ссылка потерялась (домен перезагрузился),
    /// ищем компонент на сцене, чтобы подписки не пропадали.
    /// </summary>
    public static CheckEnterkeyboard Get()
    {
        if (instance == null)
            instance = FindFirstObjectByType<CheckEnterkeyboard>();

        return instance;
    }

    private void OnDisable()
    {
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            Publish('\n');

        if (keyboard.backspaceKey.wasPressedThisFrame)
            Publish('\b');
    }

    void OnTextInput(char character)
    {
        // '\r', '\n' и '\b' приходят и из Update — отсеиваем, чтобы не было двойного срабатывания
        if (character == '\r' || character == '\n' || character == '\b')
            return;

        Publish(character);
    }

    private void Publish(char character)
    {
        if (!inputEnabled)
            return;

        currentCharacter = character;
        enterNewCharacter?.Invoke(character);
    }

    /// <summary>Включить/выключить рассылку ввода (для поля своего текста).</summary>
    public static void SetInputEnabled(bool enabled)
    {
        CheckEnterkeyboard keyboard = Get();

        if (keyboard != null)
            keyboard.inputEnabled = enabled;
    }
}

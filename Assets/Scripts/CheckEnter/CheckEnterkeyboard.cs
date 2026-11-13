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
        // После смены сцены старый объект уже живёт (DontDestroyOnLoad) — убираем дубль
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this);
    }

    private void OnEnable()
    {
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput += OnTextInput;
        }
    }

    private void OnDisable()
    {
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
        }
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
        if (instance != null)
            instance.inputEnabled = enabled;
    }
}

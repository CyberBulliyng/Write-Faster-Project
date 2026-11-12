using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CheckEnterkeyboard : MonoBehaviour
{
    public static CheckEnterkeyboard instance;

    public char currentCharacter;

    public event Action<char> enterNewCharacter;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
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

    void OnTextInput(char character)
    {
        currentCharacter = character;
        enterNewCharacter?.Invoke(character);
    }
}

using TMPro;
using UnityEngine;

/// <summary>
/// Пример подписчика: показывает введённые символы в тексте.
/// WordChecker делает то же самое (и проверку слов),
/// этот скрипт оставлен как пример работы с событием.
/// </summary>
public class testEnter : MonoBehaviour
{
    public TMP_Text testText;

    // Start вызывается после Awake у всех объектов — instance уже есть
    private void Start()
    {
        CheckEnterkeyboard.instance.enterNewCharacter += TestEnter;
    }

    private void OnDestroy()
    {
        if (CheckEnterkeyboard.instance != null)
            CheckEnterkeyboard.instance.enterNewCharacter -= TestEnter;
    }

    // Теперь событие передаёт сам символ
    public void TestEnter(char character)
    {
        testText.text += character;
    }
}

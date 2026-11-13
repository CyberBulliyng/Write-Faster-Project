using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

/// <summary>
/// Читает текст из файла: .txt (UTF-8 или Windows-1251) и .docx (Word).
/// Возвращает всегда обычный текст, готовый для набора.
/// </summary>
public static class DocumentTextReader
{
    public const int MaxCharacters = 25000;

    /// <summary>Прочитать файл и вернуть текст. Бросает исключение с понятным текстом ошибки.</summary>
    public static string Read(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            throw new FileNotFoundException("Файл не найден.");

        string extension = Path.GetExtension(path).ToLowerInvariant();

        string text;
        if (extension == ".docx")
            text = ReadDocx(path);
        else if (extension == ".txt")
            text = ReadTxt(path);
        else
            throw new NotSupportedException("Поддерживаются только .txt и .docx. Формат .doc (старый Word) прочитать нельзя.");

        text = Normalize(text);

        if (text.Length > MaxCharacters)
            throw new InvalidOperationException("В файле " + text.Length + " символов, а максимум " + MaxCharacters + ".");

        if (text.Length == 0)
            throw new InvalidOperationException("В файле нет текста.");

        return text;
    }

    /// <summary>Проверка и очистка вставленного вручную текста.</summary>
    public static string PreparePasted(string text)
    {
        text = Normalize(text ?? string.Empty);

        if (text.Length == 0)
            throw new InvalidOperationException("Поле ввода пустое.");

        if (text.Length > MaxCharacters)
            throw new InvalidOperationException("Слишком длинный текст: " + text.Length + " символов, максимум " + MaxCharacters + ".");

        return text;
    }

    /// <summary>Приводим переводы строк к одному виду, табы — к пробелам (печатать их неудобно).</summary>
    private static string Normalize(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace('\t', ' ')
            .Trim();
    }

    private static string ReadTxt(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        // 1) BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

        // 2) Пробуем строгий UTF-8
        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (ArgumentException)
        {
            // не валидный UTF-8 — значит старая кодировка
        }

        // 3) Windows-1251 (стандарт для русских txt)
        try
        {
            return Encoding.GetEncoding(1251).GetString(bytes);
        }
        catch (Exception e)
        {
            Debug.LogWarning("DocumentTextReader: cp1251 недоступен (" + e.Message + "), читаю как Latin1");
            return Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
        }
    }

    private static string ReadDocx(string path)
    {
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        using (ZipArchive archive = ZipFile.OpenRead(path))
        {
            ZipArchiveEntry entry = archive.GetEntry("word/document.xml");
            if (entry == null)
                throw new InvalidDataException("Это не похоже на документ Word (нет word/document.xml).");

            using (Stream stream = entry.Open())
            {
                XDocument document = XDocument.Load(stream);

                var paragraphs = new List<string>();

                foreach ( XElement p in document.Descendants(w + "p"))
                {
                    var sb = new StringBuilder();

                    foreach (XElement node in p.Descendants())
                    {
                        if (node.Name == w + "t")
                            sb.Append(node.Value);
                        else if (node.Name == w + "tab")
                            sb.Append(' ');
                        else if (node.Name == w + "br" || node.Name == w + "cr")
                            sb.Append('\n');
                    }

                    string text = sb.ToString().Trim();
                    if (text.Length > 0)
                        paragraphs.Add(text);
                }

                return string.Join("\n", paragraphs);
            }
        }
    }
}

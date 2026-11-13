using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

/// <summary>
/// Нативное окно «Открыть файл» для Windows без сторонних плагинов (P/Invoke на comdlg32.dll).
/// Нужен, чтобы в собранной игре игрок мог выбрать свой txt/docx.
/// </summary>
public static class WindowsOpenFileDialog
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OPENFILENAME
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public IntPtr lpstrFilter;
        public IntPtr lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public IntPtr lpstrFile;
        public int nMaxFile;
        public IntPtr lpstrFileTitle;
        public int nMaxFileTitle;
        public IntPtr lpstrInitialDir;
        public IntPtr lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public IntPtr lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public IntPtr lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int flagsEx;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetOpenFileNameW(ref OPENFILENAME ofn);

    private const int OFN_EXPLORER = 0x00080000;
    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_HIDEREADONLY = 0x00000004;

    /// <summary>
    /// Показать окно выбора файла. Возвращает путь или null, если игрок нажал «Отмена».
    /// filterExample: "Текст (*.txt;*.docx)\0*.txt;*.docx\0Все файлы (*.*)\0*.*\0\0"
    /// </summary>
    public static string Open(string title, string filter, string initialDirectory)
    {
        if (Application.platform != RuntimePlatform.WindowsPlayer &&
            Application.platform != RuntimePlatform.WindowsEditor)
        {
            Debug.LogWarning("WindowsOpenFileDialog: работает только на Windows, платформа — " + Application.platform);
            return null;
        }

        IntPtr buffer = IntPtr.Zero;
        var allocated = new System.Collections.Generic.List<IntPtr>();

        try
        {
            const int maxChars = 4096;
            buffer = Marshal.AllocHGlobal(maxChars * 2);
            allocated.Add(buffer);
            for (int i = 0; i < maxChars; i++)
                Marshal.WriteInt16(buffer, i * 2, 0);

            string seed = string.IsNullOrEmpty(initialDirectory) ? Environment.CurrentDirectory : initialDirectory;

            var ofn = new OPENFILENAME
            {
                lStructSize = Marshal.SizeOf(typeof(OPENFILENAME)),
                hwndOwner = IntPtr.Zero,
                lpstrFile = buffer,
                nMaxFile = maxChars,
                lpstrFilter = AllocUnicode(filter, allocated),
                lpstrTitle = AllocUnicode(title, allocated),
                lpstrInitialDir = AllocUnicode(seed, allocated),
                Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_HIDEREADONLY
            };

            bool ok = GetOpenFileNameW(ref ofn);
            if (!ok)
                return null; // игрок отменил

            return Marshal.PtrToStringUni(buffer);
        }
        catch (Exception e)
        {
            Debug.LogError("WindowsOpenFileDialog: не удалось открыть окно выбора файла — " + e.Message);
            return null;
        }
        finally
        {
            for (int i = 0; i < allocated.Count; i++)
            {
                if (allocated[i] != IntPtr.Zero)
                    Marshal.FreeHGlobal(allocated[i]);
            }
        }
    }

    // Копирует строку (включая встроенные \0, как в фильтре) в неуправляемую память.
    // Все выдачи складываем в список, чтобы освободить в finally.
    private static IntPtr AllocUnicode(string text, System.Collections.Generic.List<IntPtr> track)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(text ?? string.Empty);
        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length + 2);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        Marshal.WriteInt16(ptr, bytes.Length, 0);
        track.Add(ptr);
        return ptr;
    }
}

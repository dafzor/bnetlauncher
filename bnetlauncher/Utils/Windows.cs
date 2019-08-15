using System;
using System.Runtime.InteropServices;
using System.Text;

namespace bnetlauncher.Utils
{
    public static class Windows
    {
        internal static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString,int nMaxCount);
        }


        static public bool IsForegroundWindowTitle(string title)
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();

            int length = NativeMethods.GetWindowTextLength(hwnd);
            StringBuilder windowtitle = new StringBuilder(length + 1);
            int result_len = NativeMethods.GetWindowText(hwnd, windowtitle, windowtitle.Capacity);

            if (result_len != length)
            {
                Logger.Warning($"Got missmatched Windows title length. {length} vs {result_len}");
                return false;
            }

            Logger.Information($"Foreground Window title = '{windowtitle.ToString()}'");
            return (windowtitle.ToString().Equals(title, StringComparison.OrdinalIgnoreCase));
        }
    }
}

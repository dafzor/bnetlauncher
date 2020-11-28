using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace bnetlauncher.Utils
{
    public static class WinApi
    {
        internal static class NativeMethods
        {
            // Windows Event KeyDown
            public const int WM_KEYDOWN = 0x100;

            // Constant for Enter Key
            public const int VK_RETURN = 0x0D;

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString,int nMaxCount);

            [DllImport("User32.dll")]
            public static extern int SetForegroundWindow(IntPtr point);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        }

        #region Unusued functions
        //static public bool IsForegroundWindowByTitle(string title)
        //{
        //    IntPtr hwnd = NativeMethods.GetForegroundWindow();

        //    int length = NativeMethods.GetWindowTextLength(hwnd);
        //    StringBuilder windowtitle = new StringBuilder(length + 1);
        //    int result_len = NativeMethods.GetWindowText(hwnd, windowtitle, windowtitle.Capacity);

        //    if (result_len != length)
        //    {
        //        Logger.Warning($"Got missmatched Windows title length. {length} vs {result_len}");
        //        return false;
        //    }

        //    Logger.Information($"Foreground Window title = '{windowtitle.ToString()}'");
        //    return (windowtitle.ToString().Equals(title, StringComparison.OrdinalIgnoreCase));
        //}

        //static public bool SetForegroundWindowByTitle(string title)
        //{
        //    try
        //    {
        //        var client = Process.GetProcessesByName(title)[0];
        //        return NativeMethods.SetForegroundWindow(client.MainWindowHandle) != 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error($"Exception wile trying to bring '{title}' to the foreground.", ex);
        //    }

        //    return false;
        //}

        //static public bool SetForegroundWindowByHandle(IntPtr handle)
        //{
        //    if (handle != null)
        //    {
        //        return NativeMethods.SetForegroundWindow(handle) != 0;
        //    }
        //    return false;
        //}

        //static public bool SendEnterByTitle(string title)
        //{
        //    var windows = Process.GetProcesses();

        //    bool sent = false;
        //    foreach (var window in windows)
        //    {
        //        if (window.MainWindowTitle == title)
        //        {
        //            NativeMethods.SendMessage(window.MainWindowHandle,
        //                NativeMethods.WM_KEYDOWN, NativeMethods.VK_RETURN, IntPtr.Zero);

        //            Logger.Information($"Sending Enter to '{window.MainWindowTitle}'");
        //            sent = true;
        //        }
        //    }

        //    return sent;            
        //}
        #endregion unused functions

        static public void SendEnterByHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                Logger.Error($"Given null handle. aborting...");
                return;
            }

            Logger.Information("Sending enter key to window");
            NativeMethods.SendMessage(handle, NativeMethods.WM_KEYDOWN, (IntPtr)NativeMethods.VK_RETURN, IntPtr.Zero);
        }
    }
}

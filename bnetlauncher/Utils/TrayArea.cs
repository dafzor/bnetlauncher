using System;
using System.Runtime.InteropServices;

namespace bnetlauncher.Utils
{
    /// <summary>
    /// Class copied from:
    /// https://maruf-dotnetdeveloper.blogspot.com/2012/08/c-refreshing-system-tray-icon.html
    /// All rights bellong to original author
    /// </summary>
    public static class TrayArea
    {
        internal static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        }

        /// <summary>
        /// Refreshes the TrayArea removing any lingering TrayIcon.
        /// </summary>
        public static void Refresh()
        {
            IntPtr systemTrayContainerHandle = NativeMethods.FindWindow("Shell_TrayWnd", null);
            IntPtr systemTrayHandle = NativeMethods.FindWindowEx(systemTrayContainerHandle, IntPtr.Zero, "TrayNotifyWnd", null);
            IntPtr sysPagerHandle = NativeMethods.FindWindowEx(systemTrayHandle, IntPtr.Zero, "SysPager", null);
            IntPtr notificationAreaHandle = NativeMethods.FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32", "Notification Area");

            if (notificationAreaHandle == IntPtr.Zero)
            {
                notificationAreaHandle = NativeMethods.FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32", "User Promoted Notification Area");
                IntPtr notifyIconOverflowWindowHandle = NativeMethods.FindWindow("NotifyIconOverflowWindow", null);
                IntPtr overflowNotificationAreaHandle = NativeMethods.FindWindowEx(notifyIconOverflowWindowHandle, IntPtr.Zero, "ToolbarWindow32", "Overflow Notification Area");
                RefreshTrayArea(overflowNotificationAreaHandle);
            }
            RefreshTrayArea(notificationAreaHandle);
        }


        private static void RefreshTrayArea(IntPtr windowHandle)
        {
            const UInt32 wmMousemove = 0x0200;
            NativeMethods.GetClientRect(windowHandle, out NativeMethods.Rect lpRect);
            for (var x = 0; x < lpRect.right; x += 5)
                for (var y = 0; y < lpRect.bottom; y += 5)
                    NativeMethods.SendMessage(windowHandle, wmMousemove, IntPtr.Zero, (IntPtr)(y << 16) + x);
        }
    }
}
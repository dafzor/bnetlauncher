using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

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

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;        // x position of upper-left corner
                public int Top;         // y position of upper-left corner
                public int Right;       // x position of lower-right corner
                public int Bottom;      // y position of lower-right corner
            }

#pragma warning disable 649
            public struct INPUT
            {
                public UInt32 Type;
                public MOUSEKEYBDHARDWAREINPUT Data;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct MOUSEKEYBDHARDWAREINPUT
            {
                [FieldOffset(0)]
                public MOUSEINPUT Mouse;
            }

            public struct MOUSEINPUT
            {
                public Int32 X;
                public Int32 Y;
                public UInt32 MouseData;
                public UInt32 Flags;
                public UInt32 Time;
                public IntPtr ExtraInfo;
            }

#pragma warning restore 649

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

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool SetProcessDPIAware();

            [DllImport("user32.dll")]
            public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

            [DllImport("user32.dll")]
            public static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

            [DllImport("user32.dll")]
            public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);
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

        // https://stackoverflow.com/questions/30965343/printwindow-could-not-print-google-chrome-window-chrome-widgetwin-1
        public static Bitmap CaptureProcessMainWindow(Process proc)
        {
            if (null == proc)
            {
                Logger.Error($"{nameof(proc)} is null.");
                throw new ArgumentNullException(nameof(proc));
            }
            NativeMethods.SetProcessDPIAware();

            NativeMethods.RECT wnd;
            NativeMethods.GetWindowRect(new HandleRef(proc, proc.MainWindowHandle), out wnd);

            var bmp = new Bitmap(wnd.Right - wnd.Left, wnd.Bottom - wnd.Top);  // content only

            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                IntPtr hDC = graphics.GetHdc();
                try { NativeMethods.PrintWindow(proc.MainWindowHandle, hDC, (uint)0x00000002); }
                finally { graphics.ReleaseHdc(hDC); }
            }
            return bmp;
        }

        public static Point FindColorInProcessMainWindow(Process proc, Color color,
            int xDivider = 3, int yDivider = 3)
        {
            var bmp = CaptureProcessMainWindow(proc);
            bmp.Save(Path.Combine(Path.GetTempPath(), $"{proc.ProcessName}_window_capture.bmp"));

            for (int y = bmp.Height - 1; y > (bmp.Height / yDivider); y--)
            {
                for (int x = bmp.Width - 1; x > (bmp.Width / xDivider); x--)
                {
                    var pixel = bmp.GetPixel(x, y);
                    Logger.Information($"Comparing {pixel} with {color}");
                    if (pixel == color)
                    {
                        bmp.Dispose();
                        Logger.Information($"Found color {color} in Window at {x},{y}");
                        return new Point(x, y);
                    }
                }
            }
            bmp.Dispose();
            return Point.Empty;
        }

        // https://stackoverflow.com/questions/10355286/programmatically-mouse-click-in-another-window
        public static void ClickWithinWindow(IntPtr wndHandle, Point clientPoint)
        {
            var oldPos = Cursor.Position;

            /// get screen coordinates
            NativeMethods.ClientToScreen(wndHandle, ref clientPoint);

            /// set cursor on coords, and press mouse
            Cursor.Position = new Point(clientPoint.X, clientPoint.Y);

            var inputMouseDown = new NativeMethods.INPUT();
            inputMouseDown.Type = 0; /// input type mouse
            inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

            var inputMouseUp = new NativeMethods.INPUT();
            inputMouseUp.Type = 0; /// input type mouse
            inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up

            var inputs = new NativeMethods.INPUT[] { inputMouseDown, inputMouseUp };
            _ = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));

            /// return mouse 
            Cursor.Position = oldPos;
        }
    }
}

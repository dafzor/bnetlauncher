using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace bnetlauncher.Clients
{
    class UplayClient : Client
    {
        public UplayClient()
        {
            Id = "uplay";
            Name = "UPlay";
            Exe = "upc.exe";
            MustBeRunning = true;
        }

        public override string InstallPath
        {
            get
            {
                return @"C:\Program Files (x86)\Uplay\";
            }
        }

        public override bool Launch(string cmd)
        {
            game = Process.Start(cmd);
            return true;
        }

        public override bool Start()
        {
            var client = Process.Start(Path.Combine(InstallPath, Exe));
            client.WaitForInputIdle();

            // Close the launcher window
            NativeMethods.SendMessage(client.MainWindowHandle, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            lockfile.Create();
            return true;
        }

        internal class NativeMethods
        {
            public static uint WM_CLOSE = 0x10;
            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
            public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        }
        
        private Process game;
    }
}

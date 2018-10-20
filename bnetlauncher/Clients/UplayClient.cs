// Copyright (C) 2016-2018 madalien.com
// This file is part of bnetlauncher.
//
// bnetlauncher is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// bnetlauncher is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with bnetlauncher. If not, see <http://www.gnu.org/licenses/>.
//
//
// Contact:
// daf <daf@madalien.com>

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using bnetlauncher.Utils;
using System.Threading;
using System.Management;

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
            Process game = new Process();

            Logger.Information(Path.GetDirectoryName(cmd));
            game.StartInfo.WorkingDirectory = Path.GetDirectoryName(cmd);
            game.StartInfo.FileName = cmd;

            game.Start();
            return true;
        }

        public override bool Start(bool create_lockfile = true)
        {
            if (Tasks.CreateAndRun(Id, Path.Combine(InstallPath, Exe)))
            {
                var name = Path.GetFileNameWithoutExtension(Exe);

                while (GetProcessId() == 0)
                {
                    Logger.Information($"Waiting for {Id} client to start.");
                    Thread.Sleep(100);
                }
                
                // then wait for it to fully load
                //var client = Process.GetProcessesByName(name)[0];
                //client.WaitForInputIdle();

                // Close the launcher window
                //Logger.Information($"Sending {Id} window a close message.");
                //NativeMethods.SendMessage(client.MainWindowHandle, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                if (create_lockfile) lockfile.Create();
                return true;
            }
            Logger.Error("Failed to start client.");
            return false;
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

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
using System.IO;
using System.Diagnostics;
using System.Threading;
using bnetlauncher.Utils;
using System.Windows.Forms;
using System.Drawing;

namespace bnetlauncher.Clients
{
    // This client is identical to BnetClient with except it uses an alternative launch method.
    // With it, it's possible to launch game variations such as ptr and wow classic, however it
    // relies on sending keypresses to the launch which is less reliable then the old method.
    // So BnetClient will be used if possible, otherwise BnetClient2.
    class BnetClient2: BnetClient
    {
        public BnetClient2()
        {
            Id = "battlenet2";
            Name = "Battle.net";
            Exe = "battle.net.exe";
            MustBeRunning = false;
        }

        /// <summary>
        /// Launches a battle.net client game using it's product code.
        ///
        /// Using the product code will open the client window on the apropriate
        /// tab after which enter key can be sent to start the game.
        ///
        /// </summary>
        /// <param name="cmd">Battle.net client ID command to launch.</param>
        public override bool Launch(string cmd)
        {
            Logger.Information($"Looking for installPath for '{cmd}'");
            string path = GetProductInstallPath(cmd);
            if (String.IsNullOrEmpty(path))
            {
                Logger.Error($"Couldn't find install path for {cmd}");
                return false;
            }

            try
            {
                // This is the launch parameters used by the blizzard launchers like "World of Warcraft Launcher.exe"
                // While the --game parameter is not supposed to be product code but a specific string like "wow_enus",
                // "diablo3_enus" or "hs_beta" which means there's no pattern to it or location i can extract it from.
                //
                // However during testing while --game is required it's content doesn't seem to be used for anything
                // putting in any string would still launch the game correctly, so we just fill the product code so
                // it's not empty.
                // The other two fields, gamepath and productcode must be correct though.
                Process p = Process.Start(Path.Combine(InstallPath, Exe), $"--game={cmd} --gamepath=\"{path}\" --productcode={cmd}");
                p.WaitForExit();

                // Only wait for the battle.net client window to be in the foreground for
                // a minute, otherwise exit
                DateTime start = DateTime.Now;
                while (DateTime.Now.Subtract(start).TotalMinutes < 1)
                {
                    // Agressivly scans for the battle.net client window to hit the foreground
                    foreach (var proc in Process.GetProcesses())
                    {
                        if (proc.MainWindowTitle == "Battle.net")
                        {
                            Logger.Information("Found windows for battle.net client.");

                            // Small pause to give time for UI to update before
                            // sending the keypress, no wait will case it to launch
                            // the last game opened.
                            Thread.Sleep(500);

                            // To get this color check debug bmp in Program.DataPath
                            var button_color = Color.FromArgb(255, 0, 116, 224);
                            while (proc.MainWindowHandle == IntPtr.Zero)
                            {
                                Thread.Sleep(500);
                            }


                            var button_location = Point.Empty;
                            for (int i = 0; i < 3; i++)
                            {
                                button_location = WinApi.FindColorInProcessMainWindow(proc, button_color);
                                if (button_location != Point.Empty)
                                {
                                    break;
                                }
                                Thread.Sleep(100);
                            }
                            Logger.Information("Sending Mouse click at window");
                            WinApi.ClickWithinWindow(proc.MainWindowHandle, button_location);
                            return true;
                        }
                    }
                }

                Logger.Error("Failed to detect Battle.net client window in the foreground.");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Couldn't start game using '{cmd}'.", ex);
                return false;
            }
        }
    }
}

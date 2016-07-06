// Copyright (C) 2016 madalien.com
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
// along with Foobar. If not, see <http://www.gnu.org/licenses/>.
//
//
// Contact:
// daf <daf@madalien.com>
//
// References:
// https://www.reddit.com/r/Overwatch/comments/3tfrv5/guide_how_to_use_steam_overlay_with_the_blizzard/
// http://www.swtor.com/community/showthread.php?t=94152
// https://msdn.microsoft.com/en-us/library/aa394372(v=vs.85).aspx
//
// Release Changes:
// 1.4 - Cleanup and licensing of the code for public release
// 1.3 - Fully functional public release


using System;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace bnetlauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            // Needed so when we show a Messagebox it doesn't look like Windows 98
            Application.EnableVisualStyles();

            // TODO BUG: Must account for multiple instances of the launcher running at the same time
            // TODO BUG: Better resolution on the log timestamps and identify the pid it belongs to
            // TODO BUG: What happens when the b.net process dies while we checking for child processes?
            // TODO BUG: Add aditional flag to enable log appending

            Logger(String.Format("{0} version {1} started",
                Application.ProductName,
                Application.ProductVersion),
                false);

            // Parse given arguments
            string bnet_cmd = "battlenet://";
            if (args.Length > 0)
            {
                // TODO: Maybe it would be nice to try and correct bad capitalization on arguments?
                bnet_cmd += args[0].Trim();
                Logger("Using parameter: " + bnet_cmd);
            }
            else
            {
                string message = "Use one of the following *case sensitive* parameters to launch the game:\n" +
                    "WoW\t= World of Warcraft\n" +
                    "D3\t= Diablo 3\n" +
                    "WTCG\t= Heartstone\n" +
                    "Pro\t= Overwatch\n" +
                    "S2\t= Starcraft 2\n" +
                    "Hero\t= Heroes of the Storm\n";

                MessageBox.Show(message, "Howto Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger("No parameter given, exiting");
                return; // Exit Application
            }

            // Flag to close the client on exit or not.
            bool bnet_close = false;

            // TODO: Find a way to start b.net launcher without steam attaching overlay

            // Is battle.net open? If no then open it up and mark it for closure as soon as we launch the game
            if (GetBnetProcessId() == 0)
            {
                Logger("battle.net client not found, starting it");
                Process.Start("battlenet://");
                bnet_close = true;
            }

            // HACK: battle.net launcher starts two helper child process, until they're up and running the launch
            //       command is ignored so we need to wait for them before trying to launch the game so we loop
            //       until we see them running.

            // TODO: Add a timeout so we don't get stuck in an endless loop in case battle.net launcher never
            //       starts properly.
            Logger("Waiting for bnet client to start");
            while (Process.GetProcessesByName("Battle.net Helper").Length < 2)
            {
                Thread.Sleep(100);
            }
            Logger("battle.net client fully running");

            // Fire up game trough battle.net using the built in uri handler, we take the date to make sure we
            // close the game we launched and not a game that might already be running.
            DateTime client_start_date = DateTime.Now;
            Process.Start(bnet_cmd);

            // Waits for the client to start trough battle.net
            Logger("Searching for new battle.net child process after date = " + client_start_date);
            var game_process_id = 0;
            do
            {
                // TODO: Safety check to avoid getting stuck in an infinit loop?
                game_process_id = GetLastBnetProcessIdSinceDate(client_start_date);
            } while (game_process_id == 0);
            Logger("battle.net child process found with pid = " + game_process_id);


            // Copies the game process arguments to launch a second copy of the game under this program and kills
            // the current game process that's under the launcher.
            Process process = new Process();
            process.StartInfo = GetProcessStartInfoById(game_process_id);

            // Failed to get parameters
            if (process.StartInfo.Arguments == "" || process.StartInfo.FileName == "")
            {
                MessageBox.Show("Failed to obtain game parameters.\nGame should start but steam overlay won't be attached to it.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit Application
            }

            Logger("Closing battle.net child process and starting it under bnetlauncher");
            try
            {
                Process.GetProcessById(game_process_id).Kill();
                process.Start();
            }
            catch (Exception ex)
            {
                Logger(ex.ToString());
                MessageBox.Show("Failed to relaunch game under bnetlauncher/steam.\nOverlay will not work.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // TODO BUG: Only close b.net launcher if a there isn't a second instance of bnetlauncher running.
            // TODO BUG: Add try catch to killing the bnet launcher

            // Close client if it wasn't started
            if (bnet_close)
            {
                Logger("Killing battle.net client");
                Process.GetProcessById(GetBnetProcessId()).Kill();
            }

            Logger("Exiting");
            return;
        }

        /// <summary>
        /// Returns a filled ProcessStartInfo class with the arguments used to launch the process with the given id.
        /// The function will try retry_count before giving up and trowing an exeption. Each retry waits 100ms.
        /// </summary>
        /// <param name="process_id">Process Id of the process which arguments you want copied.</param>
        /// <param name="retry_count">The number of times it will try to adquire the information before it fails. Defaults to 100 tries.</param>
        /// <returns>ProcessStartInfo with FileName and Arguments set to the same ones used in the given process id.</returns>
        private static ProcessStartInfo GetProcessStartInfoById(int process_id, int retry_count = 100)
        {
            var start_info = new ProcessStartInfo();

            // IMPORTANT: If the game is slow to launch (computer just booted), it's possible that it will return a pid but then
            //            fail to retrive the start_info, thus we do this retry cycle to make sure we actually get the information
            //            we need.
            int retry = 1;
            bool done = false;
            while (retry < retry_count && done != true)
            {
                Logger(String.Format("Attempt {0} to find start parameters", retry));
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT CommandLine, ExecutablePath FROM Win32_Process WHERE ProcessId = " +
                        process_id.ToString()))
                    {
                        foreach (var result in searcher.Get())
                        {
                            start_info.FileName = result["ExecutablePath"].ToString();

                            var command_line = result["CommandLine"].ToString();
                            var cut_off = start_info.FileName.Length;

                            // We do this to remove the the first wow exe from the arguments plus "" if present
                            if (command_line[0] == '"')
                            {
                                cut_off += 2;
                            }
                            start_info.Arguments = command_line.Substring(cut_off);
                            done = true;
                            break;
                        }

                        // TODO BUG: What happens when the b.net process dies while we checking?
                        retry += 1;
                    }
                }
                catch (Exception ex)
                {
                    Logger(String.Format("Failed attempt {0}", retry));
                    Logger(ex.ToString());
                    retry += 1;
                    Thread.Sleep(100);
                }
            }

            Logger("Filename = " + start_info.FileName);
            Logger("Arguments = " + start_info.Arguments);
            return start_info;
        }

        /// <summary>
        /// Returns the last process Id of battle.net launcher child process that's not a "battle.net helper.exe"
        /// launched after the given date.
        /// </summary>
        /// <param name="date">Date to filter from. Only processes with a greater then date will be returned.</param>
        /// <returns>Process Id of the child process.</returns>
        private static int GetLastBnetProcessIdSinceDate(DateTime date)
        {

            var last_process_id = 0;
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_Process WHERE " +
                "CreationDate > '" + ManagementDateTimeConverter.ToDmtfDateTime(date).ToString() + "' AND " +
                "Name <> 'Battle.net Helper.exe' AND " +
                "ParentProcessId = " + GetBnetProcessId()))
            {
                foreach (var result in searcher.Get())
                {
                    var result_process_id = Convert.ToInt32(result["ProcessId"]);
                    Logger("Found battle.net child process with pid = " + result["ProcessId"]);

                    if (result_process_id > last_process_id)
                    {
                        last_process_id = result_process_id;
                    }
                }
            }
            Logger("Last battle.net child process pid = " + last_process_id);
            return last_process_id;
        }

        /// <summary>
        /// Returns the process Id of the Battle.Net.
        /// </summary>
        /// <returns>The process Id of the Battle.net launcher.</returns>
        private static int GetBnetProcessId()
        {
            // NOTE: What would happen if there's another program with a battle.net.exe running? Should we even care?
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_process WHERE Name = 'Battle.Net.exe'"))
            {
                foreach (var result in searcher.Get())
                {
                    Logger("Found running battle.net client with pid = " + result["ProcessId"]);
                    return Convert.ToInt32(result["ProcessId"]);
                }
            }

            return 0;
        }

        /// <summary>
        /// Logger function for debugging. It will output a line of text with current datetime in a log file with
        /// the same name as the exe.
        /// </summary>
        /// <param name="line">Line to write to the log</param>
        public static void Logger(String line, bool clear_file = false)
        {


            var log_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Application.CompanyName, Application.ProductName);

            // Creates directory if doesn't exist
            Directory.CreateDirectory(log_path);

            var log_file = Path.Combine(log_path, "debug.log");
            StreamWriter file = new StreamWriter(log_file, !clear_file);

            file.WriteLine("[{0}]: {1}", DateTime.Now, line);
            file.Close();
        }

        #region EXPERIMENTAL UNUSED CODE
        //// WARNING: This is experimental win32 code that was never used. It attempts to kill the battle.net process
        ////          and then send a WM_MOUSEMOVE message to the system tray so it removed the ghost icon.
        ////          I couldn't get it to work and since I personaly just leave battle.net always running I didn't
        ////          get to finish it. 
        ////          Idea taken from http://forums.codeguru.com/showthread.php?508247-RESOLVED-System-Tray-Icon-Ghost-(doesn-t-delete-after-app-is-terminated)

        //// TODO: Isolate this Win32 uglyness to it's own namespace or something.
        //[DllImport("user32.dll", SetLastError = true)]
        //public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        //[DllImport("user32.dll", SetLastError = true)]
        //public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpClassName, string lpWindowName);
        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 wMsg, IntPtr wParam, IntPtr lParam);
        //private const UInt32 WM_MOUSEMOVE = 0x0200;

        ///// <summary>
        ///// Closes the Battle.net launcher and clears out any left over ghost icons
        ///// </summary>
        //private static void CloseBnetProcess()
        //{
        //    var bnet_process = Process.GetProcessById(GetBnetProcessId());
        //    bnet_process.Kill();

        //    // TODO: Make this actually work
        //    var tray_handle = FindWindowEx(FindWindow("Shell_TrayWnd", ""), IntPtr.Zero, "TrayNotifyWnd", null);
        //    var pager_handle = FindWindowEx(tray_handle, IntPtr.Zero, "SysPager", "");
        //    var area_handle = FindWindowEx(pager_handle, IntPtr.Zero, "", "Notification Area");
        //    SendMessage(area_handle, WM_MOUSEMOVE, (IntPtr)0, (IntPtr)0);
        //}
        #endregion
    }
}

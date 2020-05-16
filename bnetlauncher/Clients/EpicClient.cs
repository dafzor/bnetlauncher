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
using System.Management;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using bnetlauncher.Utils;
using ProtoBuf;
using bnetlauncher.Utils.ProductDb;

namespace bnetlauncher.Clients
{
    class EpicClient: Client
    {
        public EpicClient()
        {
            Id = "epic";
            Name = "Epic Store";
            Exe = "EpicGamesLauncher.exe";
            MustBeRunning = false;
        }

        /// <summary>
        /// Returns the installation folder of the battle.net client using the installation path
        /// stored in the uninstall entry.
        /// 
        /// TODO: Make sure this is the best way to get the installation path now that it's so important.
        /// </summary>
        /// <returns>The path to the battle.net client folder without trailing slash</returns>
        public override string InstallPath
        {
            get
            {
                try
                {
                    // Opens the registry in 32bit mode since in 64bits battle.net uninstall entry is under Wow6432Node Key
                    using (var registry = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
                    {
                        // goes to the uninstall entry on the battle.net client and retrieves the InstallLocation key to get the path
                        using (var epic_cmd_key = registry.OpenSubKey(@"com.epicgames.launcher\shell\open\command"))
                        {
                            var epic_path = epic_cmd_key.GetValue("").ToString();
                            if (String.IsNullOrEmpty(epic_path))
                            {
                                Logger.Error("Failed to retrieve path from battle.net uninstall entry");
                            }

                            epic_path = epic_path.Replace(" %1", "").Replace("\"", "");
                            epic_path = Path.GetDirectoryName(epic_path);


                            Logger.Information($"Client InstallPath:'{epic_path}'.");
                            return epic_path;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to retrieve client InstallPath.", ex);
                    return String.Empty;
                }
            }
        }

        /// <summary>
        /// Launches a battle.net client game using it's ID.
        /// </summary>
        /// <param name="cmd">Battle.net client ID command to launch.</param>
        public override bool Launch(string cmd)
        {
            try
            {
                Process.Start(Path.Combine(InstallPath, Exe), $"com.epicgames.launcher://apps/{cmd}?action=launch");
            }
            catch (Exception ex)
            {
                Logger.Error($"Couldn't start game using '{cmd}'.", ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Starts the battle.net client and returns WaitUntilReady result.
        /// </summary>
        /// <returns>The result of the WaitUntilReady call.</returns>
        public override bool Start(bool create_lockfile = true, bool no_task = false)
        {
            // Just launches the client which is required for it to interpret launch commands properly.
            Logger.Information($"Starting '{Id}' client.");

            if (no_task)
            {
                Logger.Information("Starting the client directly.");
                Process.Start(Path.Combine(InstallPath, Exe));
            }
            else
            {
                Logger.Information("Starting the client trough task.");
                if (!Tasker.CreateAndRun(Id, Path.Combine(InstallPath, Exe)))
                {
                    Logger.Warning("Failed to start client trough task.");
                    Process.Start(Path.Combine(InstallPath, Exe));
                }
            }

            if (create_lockfile)
            {
                lockfile.Create();
            }
            // If battle.net client is starting fresh it will use a intermediary Battle.net process to start, we need
            // to make sure we don't get that process id but the actual client's process id. To work around it we wait
            // 2s before trying to get the process id. Also we wait an extra bit so that the child processes start as 
            // well (SystemSurvey.exe, Battle.net Helper.exe).
            // TODO: Find a way to do this that doesn't feel like a hack.
            Thread.Sleep(2000);
            return WaitUntilReady();
        }


        /// <summary>
        /// Waits for the given time until the battle.net client is fully started.
        /// On start Battle.net client isn't fully functional, this issuing commands to it will just do nothing, so to see if
        /// it is fully started we check for the helper processes battle.net helper. Once they start the battle.net client
        /// should be fully functional. The check is done every 500ms for the duration of the timeout.
        /// </summary>
        /// <param name="timeout">Amount of time to check if it's fully started in seconds. Default is 120s (2 minutes)</param>
        /// <returns>True if the client is fully started, false otherwise.</returns>
        protected bool WaitUntilReady(int timeout = 120)
        {
            Logger.Information("Waiting for epic client to be ready.");

            bool last_helper_started = false;

            if (GetProcessId() == 0)
            {
                Logger.Warning("Tried to WaitUntilReady with no epic client running.");
                return false;
            }

            DateTime search_start_time = DateTime.Now;
            while (!last_helper_started && DateTime.Now.Subtract(search_start_time).TotalSeconds < timeout)
            {
                try
                {
                    // Always look for the pid again because client updates or prompts might make it relaunch
                    using (var searcher = new ManagementObjectSearcher(
                        $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {GetProcessId()} AND Name LIKE 'UnrealCEFSubProcess.exe' AND CommandLine LIKE '%--renderer-client-id=4%'"))
                    {
                        last_helper_started = searcher.Get().Count > 0;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error looking for last Helper.", ex);
                }

                Thread.Sleep(100);
            }

            // Did the helpers start or did we timeout?
            if (!last_helper_started)
            {
                Logger.Error("Timeout before last Epic Helpers started.");
                return false;
            }

            // battle.net should be fully running
            Logger.Information($"Client fully running with pid:'{GetProcessId()}'");
            return true;
        }
    }
}

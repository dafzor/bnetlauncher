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
    class BnetClient: Client
    {
        public BnetClient()
        {
            Id = "battlenet";
            Name = "Battle.net";
            Exe = "battle.net.exe";
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
                    //// NOTE: Test on using the battle.net client config file instead of uninstall key.
                    //// Location of the battle.net client configuration file in JSON
                    //var bnet_config_file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    //    "Battle.net", "Battle.net.config");

                    //// Read the config file into a string
                    //var bnet_config = File.ReadAllText(bnet_config_file);

                    //// Use a Regular expression to search for the Path option to get the path.
                    //var match = Regex.Match(bnet_config, "\"Path\":.*\"(.*)\"");

                    //if (match.Success)
                    //{
                    //    // 
                    //    return match.Groups[1].Value.Replace(@"\\", @"\");
                    //}

                    // Opens the registry in 32bit mode since in 64bits battle.net uninstall entry is under Wow6432Node Key
                    using (var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                    {
                        // goes to the uninstall entry on the battle.net client and retrieves the InstallLocation key to get the path
                        using (var bnet_uninstall_key = registry.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Battle.net"))
                        {
                            var bnet_path = bnet_uninstall_key.GetValue("InstallLocation").ToString();
                            if (String.IsNullOrEmpty(bnet_path))
                            {
                                Logger.Error("Failed to retrieve path from battle.net uninstall entry");
                            }

                            Logger.Information($"Client InstallPath:'{bnet_path}'.");
                            return bnet_path;
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
                Process.Start(Path.Combine(InstallPath, Exe), $"--exec=\"launch {cmd}\"");
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
        /// Returns the number of battle.net helper processes that need to be running based on the
        /// battle.net client setting HardwareAcceleration. If true there should be at least 2 helpers
        /// otherwise only 1 is required.
        /// </summary>
        /// <returns>number of battle.net helper processes required.</returns>
        protected int GetHelperProcessCount()
        {
            // Since WoW shadowlands launch the non beta Battle.net Client requires the UI to fully
            // load before accepting commands, and disabling GPU acelaration no longers reduces the
            // thread count by one, so this value is now a constant 3, function remains in case this
            // changes again in the future.
            // NOTE: See code before tag 2.12 for previous function if needed.
            return 3;
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
            Logger.Information("Waiting for battle.net client to be ready.");

            int helper_required = GetHelperProcessCount();
            int helper_count = 0;

            if (GetProcessId() == 0)
            {
                Logger.Warning("Tried to WaitUntilReady with no battle.net client running.");
                return false;
            }

            DateTime search_start_time = DateTime.Now;
            while (helper_count < helper_required && DateTime.Now.Subtract(search_start_time).TotalSeconds < timeout)
            {
                try
                {
                    // Always look for the pid again because client updates or prompts might make it relaunch
                    using (var searcher = new ManagementObjectSearcher(
                        $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {GetProcessId()} AND Name LIKE 'Battle.net.exe'"))
                    {
                        helper_count = searcher.Get().Count;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error obtaining Helper count.", ex);
                }

                Thread.Sleep(100);
            }

            // Did the helpers start or did we timeout?
            if (helper_count < helper_required)
            {
                Logger.Error("Timeout before enough battle.net Helpers started.");
                return false;
            }

            // battle.net should be fully running
            Logger.Information($"Client fully running with pid:'{GetProcessId()}'");
            return true;
        }

        /// <summary>
        /// Gets the installPath for a given productCode.
        /// 
        /// Battle.net Agent keeps a product.db file in protoperf format containing a series
        /// of details about the games included their install location which seem to be stored
        /// in no ther location.
        /// 
        /// This function uses the product code to look for the game, best way to find it is
        /// to create a desktop shortcut for the game, launch it and then open the path
        /// 'C:\ProgramData\Battle.net\Setup' and check the log files for the code in the 
        /// launch parameters.
        /// 
        /// </summary>
        /// <param name="product_code">product code of the game to look for.</param>
        /// <returns>install path if found. Empty string otherwise.</returns>
        protected static string GetProductInstallPath(string product_code)
        {
            string db_file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                @"battle.net\Agent\product.db");
            Logger.Information($"Opening '{db_file}'");

            Database db;
            using (var file = File.OpenRead(db_file))
            {
                db = Serializer.Deserialize<Database>(file);
                foreach (ProductInstall pi in db.productInstalls)
                {
                    if (pi.productCode.Equals(product_code, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Information($"Found install path '{pi.Settings.installPath}'.");
                        return pi.Settings.installPath;

                    }
                }
            }
            return "";
        }

        /// <summary>
        /// Returns if the game is ready to launch.
        /// 
        /// File doesn't update when updating.
        /// Only at the end, so this wont work.
        /// 
        /// </summary>
        /// <param name="install_path"></param>
        /// <returns></returns>
        //protected bool IsGameReady(string install_path)
        //{
        //    string path = "";

        //    try
        //    {
        //        path = Path.Combine(install_path, ".patch.result");
        //        if (!File.Exists(path))
        //        {
        //            Logger.Error($"'{path}' doesn't exist");
        //            return false;
        //        }

        //        string state = File.ReadAllText(path);
        //        Logger.Information($"'{path}' = {state}");

        //        // possible values:
        //        // -1
        //        //  0 = ok
        //        if (state.Equals("0", StringComparison.OrdinalIgnoreCase)) {
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error($"Error reading '{path}'", ex);
        //        throw;
        //    }
        //    return false;
        //}
    }
}

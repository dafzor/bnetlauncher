// Copyright (C) 2016-2017 madalien.com
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
using System.Collections.Generic;
using Microsoft.Win32;

namespace bnetlauncher
{
    static class BnetClient
    {

        /// <summary>
        /// List BnetGame objects of Known games supported by the Launch command.
        /// </summary>
        public static List<BnetGame> Games
        {
            get
            {
                return new List<BnetGame>
                {
                    new BnetGame("WoW", "World of Warcraft", "wow"),
                    new BnetGame("D3", "Diablo 3", "d3"),
                    new BnetGame("WTCG", "Heartstone", "hs"),
                    new BnetGame("Pro", "Overwatch", "ow"),
                    new BnetGame("S2", "Starcraft 2", "sc2"),
                    new BnetGame("Hero", "Heroes of the Storm", "hots"),
                    new BnetGame("S1", "Starcraft Remastered", "scr"),
                    new BnetGame("DST2", "Destiny 2", "dst2"),
                    new BnetGame("VIPR", "Call of Duty: Black Ops 4", "codbo4")
                };
            }
        }

        /// <summary>
        /// Returns the number of battle.net helper processes that need to be running based on the
        /// battle.net client setting HardwareAcceleration. If true there should be at least 2 helpers
        /// otherwise only 1 is required.
        /// </summary>
        /// <returns>number of battle.net helper processes required.</returns>
        public static int HelperProcessCount
        {
            // Ideally I'd use a JSON library and properly parse the battle.net config file, but that
            // would add a library dependency to the project so instead we'll do the hackish alternative
            // of just regexing the config file.

            get
            {
                try
                {
                    // Location of the battle.net client configuration file in JSON
                    var bnet_config_file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Battle.net", "Battle.net.config");

                    // Read the config file into a string
                    var bnet_config = File.ReadAllText(bnet_config_file);

                    // Use a Regular expression to search for the HardwareAcceleration option and see if it's ON or OFF
                    // if it's ON then the client will have at least 2 Battle.net Helper running.
                    var match = Regex.Match(bnet_config, "\"HardwareAcceleration\":.*\"(true|false)\"");

                    if (match.Success)
                    {
                        if (match.Groups[1].Value.Equals("true"))
                        {
                            return 2;
                        }
                        else
                        {
                            // Hardware acceleration is off, so no GPU battle.net helper
                            return 1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Shared.Logger(ex.ToString());
                }

                return 2;
            }
        }

        /// <summary>
        /// Returns the installation folder of the battle.net client using the installation path
        /// stored in the uninstall entry.
        /// 
        /// TODO: Make sure this is the best way to get the installation path now that it's so important.
        /// </summary>
        /// <returns>The path to the battle.net client folder without trailing slash</returns>
        public static string InstallLocation
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
                            if (bnet_path == "")
                            {
                                Shared.Logger("Failed to retrieve path from battle.net uninstall entry");
                            }

                            return bnet_path;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Shared.Logger("Exception while trying to retrieve battle.net client path:");
                    Shared.Logger(ex.ToString());
                    return String.Empty;
                }
            }
        }

        /// <summary>
        /// Returns InstallLocation combined with battle.net.exe which seems to always the be main exe for the client now
        /// even when beta is installed.
        /// </summary>
        public static string ClientExe
        {
            get
            {
                return Path.Combine(InstallLocation, "battle.net.exe");
            }
        }

        /// <summary>
        /// Returns the process Id of the currently running Battle.Net instance.
        /// </summary>
        /// <returns>The process Id of the Battle.net launcher.</returns>
        public static int GetProcessId()
        {

            // TODO: What would happen if there's another program with a battle.net.exe running? Should we even care?
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_process WHERE Name = 'Battle.Net.Beta.exe' OR Name = 'Battle.Net.exe'"))
                {
                    foreach (var result in searcher.Get())
                    {
                        return Convert.ToInt32(result["ProcessId"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Shared.Logger(String.Format("Error finding battle.net client pid. {0}", ex.ToString()));
                return 0;
            }
            return 0;
        }

        /// <summary>
        /// Returns the first child process Id of battle.net client that's not a "battle.net helper.exe" after the
        /// given date.
        /// </summary>
        /// <param name="date">Date to filter from. Only processes with a greater then date will be returned.</param>
        /// <returns>Process Id of the child process.</returns>
        public static int GetChildProcessIdAfterDate(DateTime date, string exe = "")
        {
            int child_process_id = 0;
            DateTime child_process_date = DateTime.Now;

            var wmiq = "";
            if (exe == "")
            {
                wmiq = String.Format(
                    "SELECT ProcessId, CreationDate FROM Win32_Process WHERE CreationDate > '{0}' AND NOT (Name LIKE 'Battle.net%.exe') AND ParentProcessId = {1}",
                    ManagementDateTimeConverter.ToDmtfDateTime(date).ToString(), GetProcessId());
            }
            else
            {
                wmiq = String.Format(
                    "SELECT ProcessId, CreationDate FROM Win32_Process WHERE CreationDate > '{0}' AND Name LIKE '{1}'",
                    ManagementDateTimeConverter.ToDmtfDateTime(date).ToString(), exe);
            }

            using (var searcher = new ManagementObjectSearcher(wmiq))
            {
                foreach (var result in searcher.Get())
                {
                    var result_process_id = Convert.ToInt32(result["ProcessId"]);
                    var result_process_date = ManagementDateTimeConverter.ToDateTime(result["CreationDate"].ToString());

                    Shared.Logger(String.Format("Found battle.net child process started at '{0}' with pid = {1}", result_process_date.ToString("hh:mm:ss.ffff"), result_process_id));

                    // Closest to the given date is the one we return
                    if (result_process_date.Subtract(date).TotalMilliseconds < child_process_date.Subtract(date).TotalMilliseconds)
                    {
                        child_process_id = result_process_id;
                        child_process_date = result_process_date;
                    }
                }
            }
            if (child_process_id == 0)
            {
                Shared.Logger("No child process found.");
            }
            else
            {
                Shared.Logger(String.Format("Selecting battle.net child started at '{0}' with pid = {1}", child_process_date.ToString("hh:mm:ss.ffff"),
                    child_process_id));
            }
            return child_process_id;
        }

        /// <summary>
        /// Waits for the given time until the battle.net client is fully started.
        /// On start Battle.net client isn't fully functional, this issuing commands to it will just do nothing, so to see if
        /// it is fully started we check for the helper processes battle.net helper. Once they start the battle.net client
        /// should be fully functional. The check is done every 500ms for the duration of the timeout.
        /// </summary>
        /// <param name="timeout">Amount of time to check if it's fully started in seconds. Default is 120s (2 minutes)</param>
        /// <returns>True if the client is fully started, false otherwise.</returns>
        public static bool WaitUntilReady(int timeout = 120)
        {
            int helper_required = BnetClient.HelperProcessCount;
            int helper_count = 0;

            int bnet_pid = GetProcessId();
            if (bnet_pid == 0)
            {
                Shared.Logger("Tried to WaitUntilReady with no battle.net client running.");
                return false;
            }

            DateTime search_start_time = DateTime.Now;
            while (helper_count < helper_required && DateTime.Now.Subtract(search_start_time).TotalSeconds < timeout)
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher(
                        String.Format("SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {0} AND Name LIKE 'Battle.net%.exe'", bnet_pid)))
                    {
                        helper_count = searcher.Get().Count;
                    }
                }
                catch (Exception ex)
                {
                    Shared.Logger(ex.ToString());
                }

                Thread.Sleep(100);
            }

            // Did the helpers start or did we timeout?
            if (helper_count < helper_required)
            {
                Shared.Logger("not enough battle.net Helpers started.");
                return false;
            }

            // battle.net should be fully running
            Shared.Logger("battle.net client is fully running with pid = " + bnet_pid);
            return true;
        }

        /// <summary>
        /// Launches a battle.net client URI command (without the battlenet://). 
        /// </summary>
        /// <param name="bnet_command">Battle.net client URI command to launch without
        /// the protocol part "battlenet://", leaving it blank will launch and/or open 
        /// the battle.net client.</param>
        public static bool Launch(string bnet_command = "")
        {
            var bnet_cmd = string.Format("--exec=\"launch {0}\"", bnet_command);
            try
            {
                Process.Start(ClientExe, bnet_cmd);
            }
            catch (Exception ex)
            {
                Shared.Logger(ex.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// Starts the battle.net client and returns WaitUntilReady result.
        /// </summary>
        /// <returns>The result of the WaitUntilReady call.</returns>
        public static bool Start()
        {
            // Just launches the client which is required for it to interpret launch commands properly.
            Process.Start(ClientExe);

            // If battle.net client is starting fresh it will use a intermediary Battle.net process to start, we need
            // to make sure we don't get that process id but the actual client's process id. To work around it we wait
            // 2s before trying to get the process id. Also we wait an extra bit so that the child processes start as 
            // well (SystemSurvey.exe, Battle.net Helper.exe).
            // TODO: Find a way to do this that doesn't feel like a hack.
            Thread.Sleep(2000);

            // 
            return WaitUntilReady();
        }
    }
}

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
// along with bnetlauncher. If not, see <http://www.gnu.org/licenses/>.
//
//
// Contact:
// daf <daf@madalien.com>
//
// References:
// https://www.reddit.com/r/Overwatch/comments/3tfrv5/guide_how_to_use_steam_overlay_with_the_blizzard/
// http://www.swtor.com/community/showthread.php?t=94152
// https://msdn.microsoft.com/en-us/library/aa394372(v=vs.85).aspx
// http://stackoverflow.com/questions/5901679/kill-process-tree-programatically-in-c-sharp
// https://msdn.microsoft.com/en-us/library/yz3w40d4(v=vs.90).aspx

using System;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace bnetlauncher
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // List of known suported games bt Launch key, name and alias
            var games = new List<BnetGame>
            {
                new BnetGame("WoW", "World of Warcraft", "wow"),
                new BnetGame("D3", "Diablo 3", "d3"),
                new BnetGame("WTCG", "Heartstone", "hs"),
                new BnetGame("Pro", "Overwatch", "ow"),
                new BnetGame("S2", "Starcraft 2", "sc2"),
                new BnetGame("Hero", "Heroes of the Storm", "hots")
            };

            // Needed so when we show a Messagebox it doesn't look like Windows 98
            Application.EnableVisualStyles();

            try
            {
                // Creates data_path directory if it doesn't exist
                Directory.CreateDirectory(data_path);
            }
            catch(Exception ex)
            {
                // No Logger call since we can't even create the directory
                MessageBox.Show(String.Format("Failed to create data directory in '{0}'.\nError: {1}", data_path,
                    ex.ToString()), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit Application
            }
        
            // Iniciates the log file by setting append to false
            Logger(String.Format("{0} version {1} started", Application.ProductName, Application.ProductVersion),
                false);

            // check if WMI service is running, if it's not we wont be able to get any pid
            if (!IsWMIServiceRunning())
            {
                // The WMI service is not running, Inform the user.
                MessageBox.Show("bnetlauncher has detected that the \"Windows Management Instrumentation\" service is not running.\n" +
                    "This service is required for bnetlauncher to function properly, please make sure it's enabled, " +
                    "then try to run bnetlauncher again.\nbnetlauncher will now exit.",
                    "WMI service not running", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return; // Exit Application
            }

            // Logs generic Machine information for debugging purposes. 
            LogMachineInformation();

            // We use a Local named Mutex to keep two instances of bnetlauncher from working at the same time.
            // So we check if the mutex already exists and if so we wait until the existing instance releases it
            // otherwise we simply create it and continue.
            // This tries to avoid two instances of bnetlauncher from swapping the games they're launching.
            try
            {
                Logger("Checking for other bnetlauncher processes");
                launcher_mutex = Mutex.OpenExisting(mutex_name);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // Named Mutex doesn't exist yet, so we'll create it
                Logger("No other bnetlauncher detected");
                launcher_mutex = new Mutex(false, mutex_name);
            }
            catch (Exception ex)
            {
                // Unknown problem
                Logger(ex.ToString());
                MessageBox.Show("bnetlauncher encontered an unknown error and will now exit\n" + ex.ToString(),
                    "Unknown Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit Application
            }

            // Waits for the mutex to be released before continuing, writes a message every second for debug purposes
            // we check for time 
            var start = DateTime.Now;
            while (!launcher_mutex.WaitOne(1000))
            {
                Logger("Waiting for another bnetlauncher instance to finish.");

                // If we don't get released for over a minute it's likely something went very wrong so we quit.
                if (DateTime.Now.Subtract(start).TotalMinutes > 1)
                {
                    Logger("Waiting for over 1 minute, assuming something is wrong and exiting");
                    MessageBox.Show("A previous bnetlauncher instance seems to be stuck running.\nAborting.",
                        "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // Exit Application
                }
            }            

            // Parse the given argument
            if (args.Length <= 0)
            {
                // No parameters so just Show instructions
                string message = "Use one of the following parameters to launch a game:\n";

                foreach (var g in games)
                {
                    message += g.Alias + "\t= " + g.Name + "\n";
                }

                MessageBox.Show(message, "Howto Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger("No parameter given, exiting");
                return; // Exit Application
            }

            // Retrives the first parameter that should be the game key and checks it against the games list
            var game_key = args[0].Trim();
            Logger("Got parameter: " + game_key);

            foreach (var g in games)
            {
                if (game_key == g.Key)
                {
                    // Valid game key no need to do anything else
                    Logger("Known key for game '" + g.Name + "'");
                    break;
                }

                if (game_key.ToLower() == g.Key.ToLower())
                {
                    // Got it but it's not properly capitalized so we fix it
                    Logger("Got key in wrong case for game '" + g.Name + "'");
                    game_key = g.Key;
                    break;
                }

                if (game_key.ToLower() == g.Alias.ToLower())
                {
                    // We got one of the alias so we replace it for the actual key
                    Logger("Got valid alias for game '" + g.Name + "'");
                    game_key = g.Key;
                    break;
                }
            }

            // TODO: Find a way to start battle.net launcher without steam attaching overlay

            // Make sure battle.net client is running
            if (AssureBnetClientIsRunning() == 0)
            {
                Logger("Couldn't find the battle.net running and failed to start it. Exiting");
                MessageBox.Show("Couldn't find the battle.net running and failed to start it.\nExiting application",
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit Application
            }

            // Fire up game trough battle.net using the built in uri handler, we take the date to make sure we
            // don't mess with games that might already be running.
            DateTime launch_request_date = DateTime.Now;
            var bnet_cmd = "battlenet://" + game_key;
            Logger(String.Format("Issuing game launch command '{1}' at '{0}'", launch_request_date.ToString("hh:mm:ss.ffff"), bnet_cmd));
            Process.Start(bnet_cmd);

            // Searches for a game started trough the client for 15s
            Logger("Searching for new battle.net child processes for the game");
            int game_process_id = 0;
            while (game_process_id == 0 && DateTime.Now.Subtract(launch_request_date).TotalSeconds < 15)
            {
                game_process_id = GetBnetChildProcessIdAfterDate(launch_request_date);

                // Waits half a second to avoid weird bug where function would return pid yet would still
                // be run again for no reason.
                // TODO: Understand why ocasionaly this loops runs more then once when it returns a pid.
                Thread.Sleep(500);
            }

            if (game_process_id == 0)
            {
                Logger("No child process game found, giving up and exiting");
                MessageBox.Show("Couldn't find a game started trough battle.net Client.\n" +
                    "Please check if battle.net can launch games by opening run dialog (winkey+R) and typing: battlenet://S2\n" +
                    "battle.net client should launch Starcraft 2 or show an error about not having Starcraft 2 installed.\n" +
                    "Aborting process and exiting.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit Application
            }
        
            // Copies the game process arguments to launch a second copy of the game under this program and kills
            // the current game process that's under the battle.net client.
            var process = new Process() { StartInfo = GetProcessStartInfoById(game_process_id) };

            // Make sure our StartInfo is actually filled and not blank
            if (process.StartInfo.Arguments == "" || process.StartInfo.FileName == "")
            {
                Logger("Failed to obtain game parameters. Exiting");
                MessageBox.Show(
                    "Failed to retrive game parameters.\nGame should start but steam overlay won't be attached to it.\n" +
                    "It's likely bnetlauncher does not have enough permissions, try running bnetlauncher and steam as administrator.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit Application
            }

            try
            {
                Logger("Closing battle.net child game process and starting it under bnetlauncher");
                KillProcessAndChildren(game_process_id);
                process.Start();
            }
            catch (Exception ex)
            {
                Logger(ex.ToString());
                MessageBox.Show("Failed to relaunch game under bnetlauncher/steam.\nOverlay will not work.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Release the mutex to allow another instance of bnetlauncher to grab it and do work
            launcher_mutex.ReleaseMutex();

            // Closes the battle.net client (only if we launched it)
            CloseBnetClient();

            Logger("Exiting");
            return;
        }


        /// <summary>
        /// Queries the WMI service for it's status and returns true if it's running, false otherwise
        /// </summary>
        /// <returns>true of the WMI service is running, false otherwise</returns>
        private static bool IsWMIServiceRunning()
        {
            var sc = new System.ServiceProcess.ServiceController("Winmgmt");

            switch (sc.Status)
            {
                case System.ServiceProcess.ServiceControllerStatus.Running:
                    return true;
                    //break;

                default:
                    return false;
                    //break;
            }
        }

        /// <summary>
        /// Makes sure the battle.net client is running properly and starts it if not.
        /// </summary>
        /// <returns>returns the process id of the started battle.net client.</returns>
        private static int AssureBnetClientIsRunning()
        {
            // Is the battle.net client already running?
            int bnet_pid = GetBnetProcessId();

            // The client isn't running so let's start it
            if (bnet_pid == 0)
            {
                Logger("battle.net client not running, trying to start it");
                //StartBnetClient();
                Process.Start("battlenet://");

                // Creates a file signaling that battle.net client was started by us
                var file = File.Create(client_lock_file);

                // Explicitly close the file we just created so that when we try to delete the file 
                // it's not locked causing the next launch to also trigger a close of the client.
                file.Close();

                // If battle.net client is starting fresh it will use a intermediary Battle.net process to start, we need
                // to make sure we don't get that process id but the actual client's process id. To work around it we wait
                // 1s before trying to get the process id. Also we wait an extra bit so that the child processes start as 
                // well (SystemSurvey.exe, Battle.net Helper.exe).
                // TODO: Find a way to do this that doesn't feel like a hack.
                Thread.Sleep(2000);

                bnet_pid = GetBnetProcessId();     
            }

            // Did we actually manage to start the battle.net client or did it just timeout?
            if (bnet_pid == 0)
            {
                Logger("Failed to start battle.net client.");
                return 0; // Couldn't start the client
            }

            // On start Battle.net client isn't fully functional, this issuing commands to it will just do nothing, so we need
            // to wait for it to fully start, to do this we check for the helper processes battle.net helper. Once they start
            // the battle.net client should be fully functional.
            // We check every 500ms for 2 minutes.
            int helper_count = 0;
            int helper_required = BnetHelperRequired();
            DateTime helper_start_time = DateTime.Now;
            while (helper_count < helper_required && DateTime.Now.Subtract(helper_start_time).TotalSeconds < 120)
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher(
                        String.Format("SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {0} AND Name = 'Battle.net Helper.exe'", bnet_pid)))
                    {
                        helper_count = searcher.Get().Count;
                    }
                }
                catch (Exception ex)
                {
                    Logger(ex.ToString());
                }

                Thread.Sleep(100);
            }

            // Did the helpers start or did we timeout?
            if (helper_count < helper_required)
            {
                Logger("not enough battle.net Helpers started.");
                return 0;
            }

            // battle.net shoudl be fully running
            Logger("battle.net client is fully running with pid = " + bnet_pid);
            return bnet_pid;
        }

        /// <summary>
        /// Returns the number of battle.net helper processes that need to be running based on the
        /// battle.net client setting HardwareAcceleration. If true there should be at least 2 helpers
        /// otherwise only 1 is required.
        /// </summary>
        /// <returns>number of battle.net helper processes required.</returns>
        private static int BnetHelperRequired()
        {
            // Ideally I'd use a Json library and properly parse the battle.net config file, but that
            // would add a library dependency to the project so instead we'll do the hackish alternative
            // of just regexing the config file.

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
                        // Hardware accelaration is off, so no gpu battle.net helper
                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger(ex.ToString());
            }

            return 2;
        }

        /// <summary>
        /// Closes the battle.net client if client_lock_file exists and we're the last running instance of
        /// bnetlauncher.
        /// </summary>
        private static void CloseBnetClient()
        {
            try
            {
                // Did we start the battle.neet launcher?
                if (File.Exists(client_lock_file))
                {
                    // Wait until every other process finishes up.
                    if (launcher_mutex.WaitOne(0))
                    {
                        Logger("Closing battle.net client.");
                        KillProcessAndChildren(GetBnetProcessId());
                        File.Delete(client_lock_file);
                    }
                    else
                    {
                        Logger("mutex returned false on exit");
                    }
                }
            }
            catch(Exception ex)
            {
                Logger(ex.ToString());
            }

        }

        /// <summary>
        /// Returns a filled ProcessStartInfo class with the arguments used to launch the process with the given id.
        /// The function will try retry_count before giving up and trowing an exeption. Each retry waits 100ms.
        /// </summary>
        /// <param name="process_id">Process Id of the process which arguments you want copied.</param>
        /// <param name="retry_count">The number of times it will try to adquire the information before it fails.
        /// Defaults to 100 tries.</param>
        /// <returns>ProcessStartInfo with FileName and Arguments set to the same ones used in the given process
        /// id.</returns>
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
                    // IMPORTANT: We use System.Management API because Process.StartInfo is not populated if used on processes that we
                    //            didn't start with the Start() method. See additional information in Process.StartInfo documentation.
                    using (var searcher = new ManagementObjectSearcher("SELECT CommandLine, ExecutablePath FROM Win32_Process WHERE ProcessId = " +
                        process_id))
                    {
                        foreach (var result in searcher.Get())
                        {
                            start_info.FileName = result["ExecutablePath"].ToString();

                            // NOTICE: Working Directory needs to be the same as battle.net client uses else the games wont
                            //         start properly.
                            start_info.WorkingDirectory = Path.GetDirectoryName(result["ExecutablePath"].ToString());

                            var command_line = result["CommandLine"].ToString();

                            // We do this to remove the the first wow exe from the arguments plus "" if present
                            var cut_off = start_info.FileName.Length;
                            if (command_line[0] == '"')
                            {
                                cut_off += 2;
                            }
                            start_info.Arguments = command_line.Substring(cut_off);
                            done = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger(String.Format("Failed attempt {0}", retry));
                    Logger(ex.ToString());
                }

                retry += 1;
                Thread.Sleep(100);
            }

            Logger("Filename = " + start_info.FileName);
            Logger("Arguments = " + start_info.Arguments);
            return start_info;
        }

        /// <summary>
        /// Returns the first child process Id of battle.net client that's not a "battle.net helper.exe" after the
        /// given date.
        /// </summary>
        /// <param name="date">Date to filter from. Only processes with a greater then date will be returned.</param>
        /// <returns>Process Id of the child process.</returns>
        private static int GetBnetChildProcessIdAfterDate(DateTime date)
        {
            int child_process_id = 0;
            DateTime child_process_date = DateTime.Now;

            using (var searcher = new ManagementObjectSearcher(String.Format(
                "SELECT ProcessId, CreationDate FROM Win32_Process WHERE CreationDate > '{0}' AND Name <> 'Battle.net Helper.exe' AND ParentProcessId = {1}",
                ManagementDateTimeConverter.ToDmtfDateTime(date).ToString(), GetBnetProcessId())))
            {
                foreach (var result in searcher.Get())
                {
                    var result_process_id = Convert.ToInt32(result["ProcessId"]);
                    var result_process_date = ManagementDateTimeConverter.ToDateTime(result["CreationDate"].ToString());

                    Logger(String.Format("Found battle.net child process started at '{0}' with pid = {1}", result_process_date.ToString("hh:mm:ss.ffff"), result_process_id));

                    // Closest to the given date is teh one we return
                    if (result_process_date.Subtract(date).TotalMilliseconds < child_process_date.Subtract(date).TotalMilliseconds)
                    {
                        child_process_id = result_process_id;
                        child_process_date = result_process_date;
                    }
                }
            }
            if (child_process_id == 0)
            {
                Logger("No child process found.");
            }
            else
            {
                Logger(String.Format("Selecting battle.net child started at '{0}' with pid = {1}", child_process_date.ToString("hh:mm:ss.ffff"),
                    child_process_id));
            }
            return child_process_id;
        }

        /// <summary>
        /// Returns the process Id of the Battle.Net.
        /// </summary>
        /// <returns>The process Id of the Battle.net launcher.</returns>
        private static int GetBnetProcessId()
        {
            // TODO: What would happen if there's another program with a battle.net.exe running? Should we even care?
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_process WHERE Name = 'Battle.Net.exe'"))
                {
                    foreach (var result in searcher.Get())
                    {
                        return Convert.ToInt32(result["ProcessId"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger(String.Format("Error finding battle.net client pid. {0}", ex.ToString()));
                return 0;
            }
            return 0;
        }

        /// <summary>
        /// Struct to temporarily store Machine information retrived by LogMachineInformation
        /// </summary>
        private struct MachineInfo
        {
            public string os_name;
            public string os_bits;
            public string os_version;
            public string os_locale;
            public string cpu_name;
            //public string cpu_speed;
            //public string ram_name;
            public string ram_capacity;
            public string hdd_name;
            //public string hdd_capacity;
            public string gpu_name;
            public string gpu_driver;
            public string gpu_ram;
            //public string mb_name;            
        }

        /// <summary>
        /// Writes basic Machine information in the log for debuging purpose.
        /// </summary>
        private static void LogMachineInformation()
        {
            // This information can't be fully trusted since Windows will lie about it's version if we don't include
            // explicit support in the app.manifest. 
            Logger(String.Format("Environment: {0} ({1}), {2}", Environment.OSVersion, Environment.Version,
                (Environment.Is64BitProcess ? "64bit" : "32bit")));


            Logger("Getting Machine details:");
            var machine_info = new MachineInfo();

            try
            {
                // Operating System
                using (var searcher = new ManagementObjectSearcher("SELECT Caption, Version, OSLanguage, OSArchitecture FROM Win32_OperatingSystem"))
                {
                    foreach (var result in searcher.Get())
                    {
                        machine_info.os_name = result["Caption"].ToString();
                        machine_info.os_version = result["Version"].ToString();
                        machine_info.os_bits = result["OSArchitecture"].ToString();
                        machine_info.os_locale = result["OSLanguage"].ToString();
                    }
                }

                // Motherboard (unnecessary?)
                //using (var searcher = new ManagementObjectSearcher("SELECT Product FROM Win32_BaseBoard"))
                //{
                //    foreach (var result in searcher.Get())
                //    {
                //        machine_info.mb_name = result["Product"].ToString();
                //    }
                //}

                // CPU
                using (var searcher = new ManagementObjectSearcher("SELECT Name, CurrentClockSpeed FROM Win32_Processor"))
                {
                    foreach (var result in searcher.Get())
                    {
                        machine_info.cpu_name = result["Name"].ToString();
                        //machine_info.cpu_speed = result["CurrentClockSpeed"].ToString() + "MHz";

                    }
                }

                // RAM
                using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
                {
                    long capacity = 0;
                    foreach (var result in searcher.Get())
                    {
                        capacity += Convert.ToInt64(result["Capacity"]);
                    }
                    machine_info.ram_capacity = (capacity / Math.Pow(1024, 2)).ToString() + "MB";
                }

                // HDD
                using (var searcher = new ManagementObjectSearcher("SELECT Model FROM Win32_DiskDrive"))
                {
                    foreach (var result in searcher.Get())
                    {
                        machine_info.hdd_name += result["Model"].ToString() + ", ";
                    }

                    machine_info.hdd_name = machine_info.hdd_name.Substring(0, machine_info.hdd_name.Length - 2);
                }

                // GPU
                using (var searcher = new ManagementObjectSearcher("SELECT Caption, AdapterRAM, DriverVersion FROM Win32_VideoController"))
                {
                    foreach (var result in searcher.Get())
                    {
                        machine_info.gpu_name = result["Caption"].ToString();
                        // Video RAM is given in bytes so we convert it to MB
                        machine_info.gpu_ram = (Convert.ToInt64(result["AdapterRAM"]) / Math.Pow(1024, 2)).ToString() + "MB";
                        machine_info.gpu_driver = result["DriverVersion"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger(String.Format("Error Getting Machine information. {0}", ex.ToString()));
            }

            Logger(String.Format("OS: {0} ({1}, {2}, {3})", machine_info.os_name, machine_info.os_version,
                machine_info.os_bits, machine_info.os_locale));
            //Logger(String.Format("MB: {0}", machine_info.mb_name));
            Logger(String.Format("CPU: {0}; RAM: {1}", machine_info.cpu_name, machine_info.ram_capacity));
            Logger(String.Format("GPU: {0} ({2}, {1})", machine_info.gpu_name, machine_info.gpu_driver, machine_info.gpu_ram));
            //Logger(String.Format("RAM: {0}", machine_info.ram_capacity));
            Logger(String.Format("HDD: {0}", machine_info.hdd_name));

        }

        /// <summary>
        /// Kill a process tree recursivly
        /// </summary>
        /// <param name="process_id">Process ID.</param>
        private static void KillProcessAndChildren(int process_id)
        {
            using (var searcher = new ManagementObjectSearcher(
                String.Format("SELECT * FROM Win32_Process WHERE ParentProcessId = {0}", process_id)))
            {
                foreach (var result in searcher.Get())
                {
                    KillProcessAndChildren(Convert.ToInt32(result["ProcessID"]));
                }
                try
                {
                    Process process = Process.GetProcessById(process_id);
                    process.Kill();
                }
                catch(ArgumentException)
                {
                    // Process already exited.
                }
                catch(Exception ex)
                {
                    Logger(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Logger function for debugging. It will output a line of text with current datetime in a log file per
        /// instance. The log file will only be writen if a "enablelog" or "enablelog.txt" file exists in the
        /// data_path.
        /// </summary>
        /// <param name="line">Line to write to the log</param>
        /// <param name="append">Flag that sets if the line should be appended to the file. First use should be
        /// false.</param>
        public static void Logger(String line, bool append = true)
        {
            if (!File.Exists(Path.Combine(data_path, "enablelog")) &&
                !File.Exists(Path.Combine(data_path, "enablelog.txt")) &&
                !File.Exists(Path.Combine(data_path, "enablelog.txt.txt")))
            {
                // only enable logging if a file named enablelog exists in 
                return;
            }

            var log_file = Path.Combine(data_path, "debug_" +
                Process.GetCurrentProcess().StartTime.ToString("yyyyMMdd_HHmmssffff") +".log");

            StreamWriter file = new StreamWriter(log_file, append);
            file.WriteLine("[{0}]: {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"),
                line);
            file.Close();
        }

        /// <summary>
        /// Path used to save the debug logs and client_lock_file
        /// </summary>
        private static string data_path = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), Application.CompanyName, "bnetlauncher");

        /// <summary>
        /// File that serves as a lock to signal battle.net client was started by bnetlauncher.
        /// </summary>
        private static string client_lock_file = Path.Combine(data_path, "bnetlauncher_startedclient.lock");

        /// <summary>
        /// Global named mutex object
        /// </summary>
        private static Mutex launcher_mutex;

        /// <summary>
        /// Constant String that identifies the named mutex.
        /// </summary>
        private const string mutex_name = "Local\\madalien.com_bnetlauncher_running";
    }
}

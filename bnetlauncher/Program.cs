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
//
// References:
// https://www.reddit.com/r/Overwatch/comments/3tfrv5/guide_how_to_use_steam_overlay_with_the_blizzard/
// http://www.swtor.com/community/showthread.php?t=94152
// https://msdn.microsoft.com/en-us/library/aa394372(v=vs.85).aspx (Win32_Process class)
// http://stackoverflow.com/questions/5901679/kill-process-tree-programatically-in-c-sharp
// https://msdn.microsoft.com/en-us/library/yz3w40d4(v=vs.90).aspx (Mutex.OpenExisting Method (String, MutexRights))
// https://msdn.microsoft.com/en-us/library/aa767914(v=vs.85).aspx (Registering an Application to a URI Scheme)
// https://stackoverflow.com/questions/2039186/reading-the-registry-and-wow6432node-key
//
// Starting the battle.net client unattached from the Steam Overlay
// ================================================================
// This can probably be achieved by using Task Scheduler and creating a task that starts
// the battle.net client. It could be used to start the battle.net client with the game
// and leave it open. Don't think anyone needs this so leaving the research here in case
// someone asks for it later.
// https://stackoverflow.com/questions/7394806/creating-scheduled-tasks
//
// Ideas that might or may not be implemented
// ==========================================
// * add code to repair battlenet URI association (fix some people having it broken)
// * start battle.net client trough task scheduler (so overlay doesn't get attached to the launcher)
// * implement a reusable Form to replace MessageBox (easier to copy text, additional functionality, etc) 
// * logger viewer on error and send report to author button (streamline issue reporting)
// * clean up for internationalization (translations)


using System;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows.Forms;

namespace bnetlauncher
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Needed so when we show a message box it doesn't look like Windows 98
            Application.EnableVisualStyles();

            if (!Shared.CreateDataPath())
            {
                // No Logger call since we can't even create the directory
                ShowMessageAndExit(String.Format("Failed to create data directory in '{0}'.\n", Shared.DataPath),
                    "Error: Write Access");
                // Can't do a Logger call since we have no write access
            }
        
            // Initiates the log file by setting append to false
            Shared.Logger(String.Format("{0} version {1} started", Application.ProductName, Application.ProductVersion), false);

            // check if WMI service is running, if it's not we wont be able to get any process ID
            if (!IsWMIServiceRunning())
            {
                Shared.Logger("WMI service not running, Exiting");
                // The WMI service is not running, Inform the user.
                ShowMessageAndExit("The \"Windows Management Instrumentation\" service is not running.\n" +
                    "This service is required for bnetlauncher to function properly, please make sure it's enabled, before trying again.",
                    "WMI service not running");
            }

            // Logs generic Machine information for debugging purposes. 
            LogMachineInformation();


            // Checks if the battle.net client installLocation property is not returning an empty path
            
            if (BnetClient.InstallLocation == String.Empty)
            {
                ShowMessageAndExit("Couldn't retrive Battle.net Client install location.\n\n" +
                  "Please reinstall the Battle.net Client to fix the issue\n");
            }

            // logging the client used in case something weird happens...
            Shared.Logger(String.Format("ClientExe = '{0}'", BnetClient.ClientExe)); 

            // Checks if the battle.net client exe exists
            if (!File.Exists(BnetClient.ClientExe))
            {
                ShowMessageAndExit("Couldn't find the Battle.net Client exe in the following location:\n" +
                    "'" + BnetClient.ClientExe + "'\n\n" +
                    "Please check if Battle.net Client is properly Installed.");
            }

            // We use a Local named Mutex to keep two instances of bnetlauncher from working at the same time.
            // So we check if the mutex already exists and if so we wait until the existing instance releases it
            // otherwise we simply create it and continue.
            // This tries to avoid two instances of bnetlauncher from swapping the games they're launching.
            try
            {
                Shared.Logger("Checking for other bnetlauncher processes");
                launcher_mutex = Mutex.OpenExisting(mutex_name);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // Named Mutex doesn't exist yet, so we'll create it
                Shared.Logger("No other bnetlauncher detected");
                launcher_mutex = new Mutex(false, mutex_name);
            }
            catch (Exception ex)
            {
                // Unknown problem
                Shared.Logger(ex.ToString());
                ShowMessageAndExit("A mutex exception has occurred:\n" + ex.ToString(),
                    "Mutex Exception");
            }

            // Waits for the mutex to be released before continuing, writes a message every second for debug purposes
            // we check for time 
            var start = DateTime.Now;
            while (!launcher_mutex.WaitOne(1000))
            {
                Shared.Logger("Waiting for another bnetlauncher instance to finish.");

                // If we don't get released for over a minute it's likely something went very wrong so we quit.
                if (DateTime.Now.Subtract(start).TotalMinutes > 1)
                {
                    Shared.Logger("Waiting for over 1 minute, assuming something is wrong and exiting");
                    ShowMessageAndExit("A previous bnetlauncher instance seems to have not properly exited.\n" +
                        "Try using Windows Task Manager to Close it and try again, if the problem persists " +
                        "report the issue to bnetlauncher author.",
                        "Stuck Instance");
                }
            }            

            // Parse the given arguments
            if (args.Length <= 0)
            {
                // No parameters so just Show instructions
                var message = "No Launch Option has been set.\n" +
                    "To launch a game please add one of the following to the launch options:\n";

                foreach (var g in BnetClient.Games)
                {
                    message += g.Alias + "\t= " + g.Name + "\n";
                }

                Shared.Logger("No parameter given, exiting");
                ShowMessageAndExit(message, "How to Use", MessageType.Info);
            }

            // Check if the ignore_key flag is passed as a second parameter
            var param_ignore = false;
            if (args.Length > 1)
            {
                var option = args[1].ToLower().Trim();
                param_ignore = (option == "-i" || option == "/i");  
            }

            // Retrieves the first parameter that should be the game key and checks it against the games list
            //  and looks for the key given in our games list, in an attempt to avoid user mistakes we
            // clean the input by forcing lowercase and strip - and / before comparing it to know alias.
            var param_game = args[0].Trim();
            var param_game_clean = param_game.Replace("-", "").Replace("/", "").ToLower();
            Shared.Logger("Given parameter: " + param_game);

            var game_key = "";
            foreach (var g in BnetClient.Games)
            {
                if (param_game_clean == g.Alias || param_game_clean == g.Key)
                {
                    // We got a valid alias so we replace it for the actual key
                    // set the found_key to true and stop the search.
                    Shared.Logger("Known key for game '" + g.Name + "'");
                    game_key = g.Key;
                    break;
                }
            }

            // If the key isn't a know alias and if the ignore flag is not set give a warning about
            // invalid key.
            if (game_key == "" && !param_ignore)
            {
                Shared.Logger(String.Format("Invalid key '{0}' given and ignore flag not set, exiting.", param_game));

                var message = String.Format("Unknown launch option '{0}' given.\n", param_game);
                message += "\nPlease use one of the know launch options:\n";
                foreach (var g in BnetClient.Games)
                {
                    message += g.Alias + "\t= " + g.Name + "\n";
                }
                message += "\nIf this is really the launch option you wish to use add ' -i' after it " +
                    " to ignore this check and use it anyway, or contact the author to add it.\n" +
                    "bnetlauncher will now Close.\n";

                ShowMessageAndExit(message, "Unknown Launch Option");
            }
            if (game_key == "" && param_ignore)
            {
                Shared.Logger(String.Format("Unknown parameter {0} given with ignore flag set, continuing", param_game));
                game_key = param_game;
            }

            // Make sure battle.net client is running
            if (BnetClient.GetProcessId() == 0)
            {
                // Start the client
                if (BnetClient.Start())
                {
                    // Creates a file signaling that battle.net client was started by us.
                    // We explicitly call close on the file we just created so that when we try to delete the file 
                    // it's not locked causing the next launch to also trigger a close of the client.
                    File.Create(client_lock_file).Close();
                }
                else
                {
                    Shared.Logger("battle.net not running and failed to start it. Exiting");
                    ShowMessageAndExit("Couldn't find the battle.net running and failed to start it.\nExiting application",
                        "Client not found");
                }
            }

            // Fire up game trough battle.net using the built in URI handler, we take the date to make sure we
            // don't mess with games that might already be running.
            DateTime launch_request_date = DateTime.Now;
            Shared.Logger(String.Format("Issuing game launch command '{1}' at '{0}'", launch_request_date.ToString("hh:mm:ss.ffff"), game_key));
            BnetClient.Launch(game_key);

            // Searches for a game started trough the client for 15s
            Shared.Logger("Searching for new battle.net child processes for the game");
            int game_process_id = 0;
            while (game_process_id == 0 && DateTime.Now.Subtract(launch_request_date).TotalSeconds < 15)
            {
                game_process_id = BnetClient.GetChildProcessIdAfterDate(launch_request_date);

                // Waits half a second to avoid weird bug where function would return process ID yet would still
                // be run again for no reason.
                // TODO: Understand why occasionally this loops runs more then once when it returns a process ID.
                Thread.Sleep(500);
            }

            if (game_process_id == 0)
            {
                Shared.Logger("No child process game found, giving up and exiting");

                // Exit Application
                ShowMessageAndExit("Couldn't find a game started trough battle.net Client.\n" +
                    "Please check if battle.net did not encounter an error and the game can be launched " +
                    "normally from the battle.net client.\n\nbnetlauncher will now exit.",
                    "Error: Game not found");
            }
        
            // Copies the game process arguments to launch a second copy of the game under this program and kills
            // the current game process that's under the battle.net client.
            var process = new Process() { StartInfo = GetProcessStartInfoById(game_process_id) };

            // Make sure our StartInfo is actually filled and not blank
            if (process.StartInfo.Arguments == "" || process.StartInfo.FileName == "")
            {
                Shared.Logger("Failed to obtain game parameters. Exiting");

                // Exit Application in error
                ShowMessageAndExit("Failed to retrieve game parameters.\nGame might start but steam overlay won't be attached to it.\n" +
                    "This can happen if the game is no longer running (Starcraft Remastered can only have one running instance) " +
                    "or when bnetlauncher does not have enough permissions, try running bnetlauncher and steam as administrator.",
                    "Game Parameters");
            }

            try
            {
                Shared.Logger("Closing battle.net child game process and starting it under bnetlauncher");
                KillProcessAndChildren(game_process_id);
                process.Start();
            }
            catch (Exception ex)
            {
                Shared.Logger(ex.ToString());
                ShowMessageAndExit("Failed to relaunch game under bnetlauncher/steam.\nOverlay will not work.",
                    "Failed to Launch");
            }

            // Release the mutex to allow another instance of bnetlauncher to grab it and do work
            launcher_mutex.ReleaseMutex();

            // Closes the battle.net client (only if we launched it)
            CloseBnetClientIfLast();

            // HACK: Force bnetlauncher to stick around so Destiny 2 will still show in-game status on steam.
            //       This is a bad way to do this and just works around the issue without actually fixing it.
            //       Hope to find a better solution or that this will be fixed by Destiny 2 launch.
            if (game_key == "DST2")
            {
                Shared.Logger("Waiting for destiny 2 to exit");
                process.WaitForExit();
            }

            Shared.Logger("All operations successful, exiting");
            launcher_mutex.Close();
        }

        /// <summary>
        /// Enumeration for the types of Message ShowMessageAndExit can show.
        /// </summary>
        private enum MessageType { Error, Warning, Info};

        /// <summary>
        /// Releases the mutex and shows an error message to the user, closing the mutex and exiting on okay.
        /// Note: This method will also call CloseBnetClientIfLast()
        /// </summary>
        /// <param name="message">Error message to show.</param>
        /// <param name="title">Title of the error to show. (optional)</param>
        /// <param name="type">Type of message to show, changes title suffix and icon. Defaults to Error</param>
        /// <param name="exit_code">Exit code, defaults to -1 (optional)</param>
        private static void ShowMessageAndExit(string message, string title = "", MessageType type = MessageType.Error,
            int exit_code = -1)
        {
            // Select the type of icon and suffix to add to the message
            MessageBoxIcon icon;
            string suffix;
            switch (type)
            {
                case MessageType.Info:
                    icon = MessageBoxIcon.Information;
                    suffix = "Info: ";
                    break;

                case MessageType.Warning:
                    icon = MessageBoxIcon.Warning;
                    suffix = "Warning: ";
                    break;

                default:
                    icon = MessageBoxIcon.Error;
                    suffix = "Error: ";
                    break;
            }

            try
            {
                // We hit an error, so we let the next bnetlauncher instant have a go while we show an error
                if (launcher_mutex != null) launcher_mutex.ReleaseMutex();

                // Shows the actual message
                MessageBox.Show(message, suffix + title, MessageBoxButtons.OK, icon);

                // Cleans up, makes sure the battle.net client isn't left running under steam or
                // the mutex is abandoned.

                // Did we start the battle.net launcher?
                CloseBnetClientIfLast();

                // Cleans up the mutex
                if (launcher_mutex != null) launcher_mutex.Close();
            }
            catch (Exception ex)
            {
                // ignore the two possible Exceptions
                // ApplicationException - The calling thread does not own the mutex.
                // ObjectDisposedException - The current instance has already been disposed.
                Shared.Logger("Exception: " + ex.ToString());
            }

            // calls the end of the application
            Shared.Logger("exiting with error: " + message);
            Environment.Exit(exit_code);
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
        /// Closes the battle.net client if client_lock_file exists and we're the last running instance of
        /// bnetlauncher.
        /// </summary>
        private static void CloseBnetClientIfLast()
        {
            try
            {
                // Did we start the battle.net launcher?
                if (File.Exists(client_lock_file))
                {
                    // Attempts to get a lock on the mutex immediately, if we get true we got it and there
                    // should be no other bnetlauncher running, so we clean up.
                    if (launcher_mutex.WaitOne(0))
                    {
                        Shared.Logger("Closing battle.net client.");
                        KillProcessAndChildren(BnetClient.GetProcessId());
                        File.Delete(client_lock_file);
                    }
                    else
                    {
                        Shared.Logger("mutex returned false on exit");
                    }
                }
            }
            catch (Exception ex)
            {
                Shared.Logger(ex.ToString());
            }

        }

        /// <summary>
        /// Kill a process tree recursively
        /// </summary>
        /// <param name="process_id">Process ID.</param>
        public static void KillProcessAndChildren(int process_id)
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
                catch (ArgumentException)
                {
                    // Process already exited.
                }
                catch (Exception ex)
                {
                    Shared.Logger(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Returns a filled ProcessStartInfo class with the arguments used to launch the process with the given id.
        /// The function will try retry_count before giving up and throwing an exception. Each retry waits 100ms.
        /// </summary>
        /// <param name="process_id">Process Id of the process which arguments you want copied.</param>
        /// <param name="retry_count">The number of times it will try to acquire the information before it fails.
        /// Defaults to 50 tries.</param>
        /// <returns>ProcessStartInfo with FileName and Arguments set to the same ones used in the given process
        /// id.</returns>
        private static ProcessStartInfo GetProcessStartInfoById(int process_id, int retry_count = 50)
        {
            var start_info = new ProcessStartInfo();

            // IMPORTANT: If the game is slow to launch (computer just booted), it's possible that it will return a process ID but
            //            then fail to retrieve the start_info, thus we do this retry cycle to make sure we actually get the
            //            information we need.
            int retry = 1;
            bool done = false;
            while (retry < retry_count && done != true)
            {
                Shared.Logger(String.Format("Attempt {0} to find start parameters", retry));
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

                            // We do this to remove the first wow exe from the arguments plus "" if present
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
                    Shared.Logger(String.Format("Failed attempt {0}", retry));
                    Shared.Logger(ex.ToString());
                }

                retry += 1;
                Thread.Sleep(100);
            }

            Shared.Logger("Filename = " + start_info.FileName);
            Shared.Logger("Arguments = " + start_info.Arguments);
            return start_info;
        }

        /// <summary>
        /// Struct to temporarily store Machine information retrieved by LogMachineInformation
        /// </summary>
        private struct MachineInfo
        {
            public string os_name;
            public string os_bits;
            public string os_version;
            public string os_locale;
            public string cpu_name;
            public string ram_capacity;
            public string hdd_name;
            public string gpu_name;
            public string gpu_driver;
            public string gpu_ram;            
        }

        /// <summary>
        /// Writes basic Machine information in the log for debugging purpose.
        /// </summary>
        private static void LogMachineInformation()
        {
            // This information can't be fully trusted since Windows will lie about it's version if we don't include
            // explicit support in the app.manifest. 
            Shared.Logger(String.Format("Environment: {0} ({1}), {2}", Environment.OSVersion, Environment.Version,
                (Environment.Is64BitProcess ? "64bit" : "32bit")));


            Shared.Logger("Getting Machine details:");
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

                // CPU
                using (var searcher = new ManagementObjectSearcher("SELECT Name, CurrentClockSpeed FROM Win32_Processor"))
                {
                    foreach (var result in searcher.Get())
                    {
                        machine_info.cpu_name = result["Name"].ToString();
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
                Shared.Logger(String.Format("Error Getting Machine information. {0}", ex.ToString()));
            }

            Shared.Logger(String.Format("OS: {0} ({1}, {2}, {3})", machine_info.os_name, machine_info.os_version,
                machine_info.os_bits, machine_info.os_locale));
            Shared.Logger(String.Format("CPU: {0}; RAM: {1}", machine_info.cpu_name, machine_info.ram_capacity));
            Shared.Logger(String.Format("GPU: {0} ({2}, {1})", machine_info.gpu_name, machine_info.gpu_driver, machine_info.gpu_ram));
            Shared.Logger(String.Format("HDD: {0}", machine_info.hdd_name));
        }

        /// <summary>
        /// Global named mutex object
        /// </summary>
        private static Mutex launcher_mutex = null;

        /// <summary>
        /// Constant String that identifies the named mutex.
        /// </summary>
        private const string mutex_name = "Local\\madalien.com_bnetlauncher_running";

        /// <summary>
        /// File that serves as a lock to signal battle.net client was started by bnetlauncher.
        /// </summary>
        private static string client_lock_file = Path.Combine(Shared.DataPath, "bnetlauncher_startedclient.lock");
    }
}

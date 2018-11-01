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
// Ideas and future todo list t be implemented
// ===========================================
// * implement a reusable Form to replace MessageBox (easier to copy text, additional functionality, etc) 
// * logger viewer on error and send report to author button (streamline issue reporting)
// * clean up for internationalization (translations)
// * proper command option parser
// * better code to handle multiple game options
// * even more refactoring to enable possible support for other clients like:
//     - move game launch into Client class since it's client specific
//     - move mutex code into Client class?


using System;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using IniParser;
using IniParser.Model;
using System.Reflection;
using bnetlauncher.Utils;

namespace bnetlauncher
{
    class Program
    {
        /// <summary>
        /// List of avaliable and supported clients
        /// </summary>
        static List<Client> clients = new List<Client>
        {
            new Clients.BnetClient(),

            // DISABLED: WIP not good enough to work let alone release
            //new Clients.UplayClient()
        };

        /// <summary>
        /// List of games supported.
        /// This list is loaded from an internal and external Ini.
        /// </summary>
        static List<Game> games = new List<Game>();


        /// selected game and client
        static Client selected_client;
        static Game selected_game;

        /// <summary>
        /// Time to Wait for a game to start.
        /// Can be overiden with command line parameter -t ##
        /// </summary>
        static int param_timeout = 15;


        /// <summary>
        /// Flag to leave the client open if we launch it.
        /// </summary>
        static bool param_leaveopen = false;

        static Stopwatch stopwatch = new Stopwatch();

        [STAThread]
        static void Main(string[] args)
        {
            // Needed so when we show a message box it doesn't look like Windows 98
            Application.EnableVisualStyles();

            #region System Health Checks and Log Setup
            try
            {
                // creates the datapath to make sure it exists
                Directory.CreateDirectory(DataPath);
            }
            catch(Exception ex)
            {
                Logger.Error($"Couldn't create {DataPath} directory.");

                // No Logger call since we can't even create the directory
                ShowMessageAndExit($"Failed to create data directory in '{DataPath}'.\n{ex.ToString()}",
                    "Write Access");
            }

            // Marks the begining of a new log cycle
            Logger.Information($"Starting {VersionInfo.FileDescription} v{VersionInfo.ProductVersion}");


            // check if WMI service is running, if it's not we wont be able to get any process information
            if (!IsWMIServiceRunning())
            {
                Logger.Error("WMI service not running");

                ShowMessageAndExit("The \"Windows Management Instrumentation\" service is not running.\n" +
                    "This service is required for bnetlauncher to function properly, please make sure it's enabled, before trying again.",
                    "WMI service not running");
            }

            // Logs generic System information for debugging purposes. 
            LogSystemInfo();
            #endregion

            LoadGameList();

            #region Argument Parsing
            // Parse the given arguments
            if (args.Length <= 0)
            {
                // No parameters so just Show instructions
                var message = "No Game Id has been given.\n" +
                    "To launch a game please add one of the following Ids to the launch options:\n";

                foreach (var g in games)
                {
                    message += $"{g.Id}\t= {g.Name}\n";
                }

                message += "\nSee 'instructions.txt' on how to add more games.";

                Logger.Warning("No parameter given.");
                ShowMessageAndExit(message, "How to Use", MessageType.Info);
            }

            // Check if the param_timeout is passed as a second parameter
            for (var i = 1; i < args.Length; i++)
            {
                var arg = args[i].ToLower().Trim();

                // parse options
                if (arg.StartsWith("-") || arg.StartsWith("/"))
                {
                    // remove starting character
                    arg = arg.Substring(1);

                    switch (arg)
                    {
                        case "t":
                        case "timeout":
                        case "-timeout":
                            try
                            {
                                param_timeout = Convert.ToInt32(args[++i]);
                                Logger.Information($"Changing timeout to '{param_timeout}'.");
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning($"Couldn't convert timeout:'{args[i]}' into integer, ignoring and continuing.", ex);
                            }
                            break;

                        case "l":
                        case "leaveopen":
                        case "-leaveopen":
                            Logger.Information($"Leaving the client open on exit.");
                            param_leaveopen = true;
                            break;

                        default:
                            Logger.Warning($"Ignoring unknown option: '{arg}'");
                            break;
                    }
                }
                else
                {
                    // unknown parameter
                    Logger.Warning($"Ignoring unknown parameter: '{arg}'");
                }
            }

            // Retrieves the first parameter that should be the game id and checks it against the games list
            // In an attempt to avoid user mistakes we clean the input by forcing lowercase and strip - and /
            // before comparing it to know ids.
            var param_game = args[0].Trim().Replace("-", "").Replace("/", "").ToLower();
            Logger.Information($"Given parameter '{args[0]}'.");
            selected_game = games.Find(g => g.Id == param_game);

            // If the id isn't know give a warning about invalid game.
            if (selected_game == null)
            {
                Logger.Error($"Unknown game '{param_game}'.");

                var message = $"Unknown game id '{param_game}' given.\n";
                message += "\nPlease use one of the known game ids:\n";
                foreach (var g in games)
                {
                    message += $"{g.Id}\t= {g.Name}\n";
                }
                message += $"\nPlease check if the Id exists.\n\n" +
                    "bnetlauncher will now Close.\n";

                ShowMessageAndExit(message, "Unknown Game Id");
            }
            #endregion

            // Checks if the game client exists
            selected_client = clients.Find(c => c.Id == selected_game.Client);
            if (selected_client == null)
            {
                var message = $"Unknown client '{selected_game.Client}'\n" +
                    "bnetlauncher only supports the following values:\n\n";

                foreach (var c in clients)
                {
                    message += $"  {c.Id} ({c.Name})\n";
                }
                message += "\nbnetlauncher will now exit.\n";
                ShowMessageAndExit(message, "Error: Unknown client");
            }

            Logger.Information($"Using '{selected_client.Id}' client.");

            // Checks if the client is actually Installed installLocation property is not returning an empty path
            if (!selected_client.IsInstalled)
            {
                ShowMessageAndExit($"The {selected_client.Name} client doesn't seem to be Installed.\n\n" +
                  "Please reinstall the Battle.net Client to fix the issue\n");
            }

            #region Mutex Setup to enforce single bnetlancher instance
            // We use a Local named Mutex to keep two instances of bnetlauncher from working at the same time.
            // So we check if the mutex already exists and if so we wait until the existing instance releases it
            // otherwise we simply create it and continue.
            // This tries to avoid two instances of bnetlauncher from swapping the games they're launching.
            try
            {
                Logger.Information("Checking for other bnetlauncher processes using same client");
                mutex_name += selected_client.Id;

                launcher_mutex = Mutex.OpenExisting(mutex_name);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // Named Mutex doesn't exist yet, so we'll create it
                Logger.Information("No other bnetlauncher detected");
                launcher_mutex = new Mutex(false, mutex_name);
            }
            catch (Exception ex)
            {
                // Unknown problem
                Logger.Error("Unknown error opening mutex.", ex);
                ShowMessageAndExit("A mutex exception has occurred:\n" + ex.ToString(),
                    "Mutex Exception");
            }

            // Waits for the mutex to be released before continuing, writes a message every second for debug purposes
            // we check for time 
            var start = DateTime.Now;
            while (!launcher_mutex.WaitOne(1000))
            {
                Logger.Information("Waiting for another bnetlauncher instance to finish.");

                // If we don't get released for over a minute it's likely something went very wrong so we quit.
                if (DateTime.Now.Subtract(start).TotalMinutes > 1)
                {
                    Logger.Error("Waiting for over 1 minute, assuming something is wrong and exiting");
                    ShowMessageAndExit("A previous bnetlauncher instance seems to have not properly exited.\n" +
                        "Try using Windows Task Manager to Close it and try again, if the problem persists " +
                        "report the issue to bnetlauncher author.",
                        "Stuck Instance");
                }
            }
            #endregion

            // Make sure the client is running
            if (!selected_client.IsRunning)
            {
                // Start the client
                if (!selected_client.Start(!param_leaveopen))
                {
                    Logger.Information($"Client '{selected_client.Name}' not running and/or failed to start it.");
                    ShowMessageAndExit($"Couldn't find the {selected_client.Name} running and failed to start it.\nExiting application",
                        "Client not found");
                }
            }
            else
            {
                Logger.Information($"Client already running with pid:'{selected_client.GetProcessId()}'.");
            }

            #region Launch Game
            // Fire up game trough battle.net using the built in URI handler, we take the date to make sure we
            // don't mess with games that might already be running.
            DateTime launch_request_date = DateTime.Now;


            // If nolaunch is selected don't actually launch the game but instead shows the client window and adds 1 minute to param_timeout
            if (!selected_game.Options.Contains("nolaunch"))
            {
                Logger.Information($"Issuing game launch command '{selected_game.Cmd}' at '{launch_request_date.ToString("hh:mm:ss.ffff")}'");
                selected_client.Launch(selected_game.Cmd);
            }
            else
            {
                selected_client.ShowWindow();
                param_timeout += 60;
            }

            var game_process_id = 0;
            do
            {
                // Searches for a game started trough the client for 15s
                game_process_id = Processes.GetProcessByNameAfterDate(selected_game.Exe, launch_request_date, param_timeout);

                // did we find it?
                if (game_process_id == 0)
                {
                    // didn't find the game process within timeout
                    Logger.Error($"Game '{selected_game.Id}' not found within timeout.");

                    // Game might be updating
                    var msg = $"Couldn't find {selected_game.Name} running, do you wish to keep trying?\n\n" +
                        "Canceling will exit bnetlauncher but leave the client open.\n\n" +

                        "This message is usually caused by the game being slow to start or an update being required. " +
                        "For slow starting games the launch option '-t' can also be used to increase the timeout limit.\n\n" +
                        "If you keep getting this message make sure the client is working properly and try rebooting your system.\n";

                    var answer = MessageBox.Show(msg, "Game not Found. Retry?",
                        MessageBoxButtons.RetryCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                    switch (answer)
                    {
                        case DialogResult.Retry:
                            Logger.Information("User chose to retry searching for game.");
                            continue; // back to the begining and try again

                        case DialogResult.Cancel:
                            try
                            {
                                Logger.Information("User chose to cancel.");

                                // Make it so we leave the client open
                                selected_client.WasStarted = false;

                                // Cleans up the mutex
                                if (launcher_mutex != null) launcher_mutex.Close();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Error releasing the mutex.", ex);
                            }

                            return;
                    }
                }
            }
            while (game_process_id == 0); // keep's retrying until user cancels or game found
        
            // Copies the game process arguments to launch a second copy of the game under this program and kills
            // the current game process that's under the battle.net client.
            var process = new Process() { StartInfo = Processes.GetProcessStartInfoById(game_process_id) };

            // Make sure our StartInfo is actually filled and not blank
            if (process.StartInfo.FileName == "" || (process.StartInfo.Arguments == "" && !selected_game.Options.Contains("noargs")))
            {
                Logger.Error("Failed to obtain game parameters.");

                // Exit Application in error
                ShowMessageAndExit("Failed to retrieve game parameters.\nGame might start but steam overlay won't be attached to it.\n" +
                    "This can happen if the game is no longer running (Starcraft Remastered can only have one running instance) " +
                    "or when bnetlauncher does not have enough permissions, try running bnetlauncher and steam as administrator.",
                    "Game Parameters");
            }

            try
            {
                Logger.Information("Closing game process and starting it under bnetlauncher");
                Processes.KillProcessAndChildsById(game_process_id);
                process.Start();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to relaunch game under bnetliancher.", ex);
                ShowMessageAndExit("Failed to relaunch game under bnetlauncher/steam.\nOverlay will not work.",
                    "Failed to Launch");
            }
            #endregion // Launch game

            // HACK: Add checks for uplay games that close and relaunch themselfs
            if (selected_game.Options.Contains("relaunches"))
            {
                var relaunches_timeout = 60 * 1000;
                Logger.Information("relaunches option is set, looking for new processes.");

                // the old process Id
                var old_pid = process.Id;

                Logger.Information($"{selected_game.Id} current process id is {old_pid}");

                stopwatch.Restart();                
                while (stopwatch.ElapsedMilliseconds < relaunches_timeout)
                {
                    foreach (var p in Process.GetProcessesByName(selected_game.Exe))
                    {
                        if (p.Id != old_pid)
                        {
                            process = p;
                            Logger.Information($"{selected_game.Id} new process id is {process.Id}");
                            old_pid = process.Id;
                        }
                    }
                    
                    Thread.Sleep(1000); // wait for 1s before retrying to find it
                }
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds < relaunches_timeout)
                {
                    Logger.Warning($"Couldn't find any new process for {selected_game.Id}");
                }

                Logger.Information($"Stopped looking for {selected_game.Id} processes.");
            }

            // Release the mutex to allow another instance of bnetlauncher to grab it and do work
            launcher_mutex.ReleaseMutex();

            // If we launched the client and it's not needed we can close it early
            if (!selected_client.MustBeRunning)
            {
                CloseClientIfLast();
            }

            // For games that require the client or bnetlauncher to stick around we wait
            if (selected_game.Options.Contains("waitforexit") || selected_client.MustBeRunning)
            {
                Logger.Information($"Waiting for {selected_game.Id} process {process.Id} to exit");
                process.WaitForExit();

                //// Get the process again because sometimes what we start isn't what's still running
                //int extra = 1;
                //while (extra > 0)
                //{
                //    extra = Process.GetProcessesByName(selected_game.Exe).Length;
                //    if (extra > 0)
                //    {
                //        var p2 = Process.GetProcessesByName(selected_game.Exe)[0];
                //        p2.WaitForExit();
                //    }
                //}
            }

            // Finally we close the client when we're done
            CloseClientIfLast();

            Logger.Information("All operations successful, exiting");
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
                CloseClientIfLast();

                // Cleans up the mutex
                if (launcher_mutex != null) launcher_mutex.Close();
            }
            catch (Exception ex)
            {
                // ignore the two possible Exceptions
                // ApplicationException - The calling thread does not own the mutex.
                // ObjectDisposedException - The current instance has already been disposed.
                Logger.Error("Error releasing the mutex.", ex);
            }

            // calls the end of the application
            Logger.Information($"Exiting.");
            Environment.Exit(exit_code);
        }

        /// <summary>
        /// Loads the games from a gamedb.ini file and internal settings.
        /// It will search for the files in bnetlauncher folder or it's appdata.
        /// </summary>
        private static void LoadGameList()
        {
            // Name of external gamedb file
            string[] gamedb_files =
            {
                Path.Combine(Application.StartupPath, "gamedb.ini"),
                Path.Combine(Program.DataPath, "gamedb.ini")
            };

            Logger.Information("Loading gamedb files.");
            
            var gamedb = new IniData();

            foreach (var file in gamedb_files)
            {
                // Checks if there's a gamedb.ini and loads it if  in the datapath and copies it over if there isn't one
                if (File.Exists(file))
                {
                    var ini_filedata = (new FileIniDataParser()).ReadFile(file);
                    gamedb.Merge(ini_filedata);

                    Logger.Information($"Loaded '{file}' with '{ini_filedata.Sections.Count}' games.");
                }
            }

            // Loads internal gamedb overiding the file loaded
            var ini_parser = new IniParser.Parser.IniDataParser();
            var ini_data = ini_parser.Parse(Properties.Resources.gamesdb);

            gamedb.Merge(ini_data);
            Logger.Information($"Loaded internal gamedb with '{ini_data.Sections.Count}' games.");

            // Load the gamedb into the games list
            foreach (var section in gamedb.Sections)
            {
                //TODO: Error checking?
                games.Add(new Game
                {
                    Id = section.SectionName,
                    Name = section.Keys["name"],
                    Client = section.Keys["client"],
                    Cmd = section.Keys["cmd"],
                    Exe = section.Keys["exe"],
                    Options = section.Keys["options"]
                });
            }

            Logger.Information($"Known games: '{games.Count}'.");
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
        private static void CloseClientIfLast()
        {
            try
            {
                // Did we start the battle.net launcher?
                if (selected_client.WasStarted)
                {
                    // Attempts to get a lock on the mutex immediately, if we get true we got it and there
                    // should be no other bnetlauncher running, so we clean up.
                    if (launcher_mutex.WaitOne(0))
                    {
                        Logger.Information($"Closing client '{selected_client.Id}'.");
                        selected_client.Close();
                    }
                    else
                    {
                        Logger.Information($"Leaving client '{selected_client.Id}' open.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to close client.", ex);
            }
        }

        /// <summary>
        /// File version information for bnetlauncher executable
        /// </summary>
        internal static FileVersionInfo VersionInfo
        {
            get
            {
                return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            }
        }

        /// <summary>
        /// Path used to save program data like the debug logs, gamedb confi and client.lock files.
        /// </summary>
        internal static string DataPath
        {
            get
            {
                // For some insane reason FileDescription has the assembly title.
                return Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData), VersionInfo.CompanyName, VersionInfo.FileDescription);
            }
        }

        /// <summary>
        /// Struct to temporarily store System information retrieved by LogSystemInfo
        /// </summary>
        private struct SystemInfo
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
        /// Writes basic System information in the log for debugging purpose.
        /// </summary>
        private static void LogSystemInfo()
        {
            Logger.Information("Getting System details:");

            // This information can't be fully trusted since Windows will lie about it's version if we don't include
            // explicit support in the app.manifest. 
            Logger.Information($"ENV: {Environment.OSVersion} ({Environment.Version}), {(Environment.Is64BitProcess ? "64" : "32")}bit");


            var sysinfo = new SystemInfo();
            try
            {
                // Operating System
                using (var searcher = new ManagementObjectSearcher("SELECT Caption, Version, OSLanguage, OSArchitecture FROM Win32_OperatingSystem"))
                {
                    foreach (var result in searcher.Get())
                    {
                        sysinfo.os_name = result["Caption"].ToString();
                        sysinfo.os_version = result["Version"].ToString();
                        sysinfo.os_bits = result["OSArchitecture"].ToString();
                        sysinfo.os_locale = result["OSLanguage"].ToString();
                    }
                }

                // CPU
                using (var searcher = new ManagementObjectSearcher("SELECT Name, CurrentClockSpeed FROM Win32_Processor"))
                {
                    foreach (var result in searcher.Get())
                    {
                        sysinfo.cpu_name = result["Name"].ToString();
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
                    sysinfo.ram_capacity = (capacity / Math.Pow(1024, 2)).ToString() + "MB";
                }

                // HDD
                using (var searcher = new ManagementObjectSearcher("SELECT Model FROM Win32_DiskDrive"))
                {
                    foreach (var result in searcher.Get())
                    {
                        sysinfo.hdd_name += result["Model"].ToString() + ", ";
                    }

                    sysinfo.hdd_name = sysinfo.hdd_name.Substring(0, sysinfo.hdd_name.Length - 2);
                }

                // GPU
                using (var searcher = new ManagementObjectSearcher("SELECT Caption, AdapterRAM, DriverVersion FROM Win32_VideoController"))
                {
                    foreach (var result in searcher.Get())
                    {
                        sysinfo.gpu_name = result["Caption"].ToString();
                        // Video RAM is given in bytes so we convert it to MB
                        sysinfo.gpu_ram = (Convert.ToInt64(result["AdapterRAM"]) / Math.Pow(1024, 2)).ToString() + "MB";
                        sysinfo.gpu_driver = result["DriverVersion"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error Getting System information.", ex);
            }

            Logger.Information($"OS: {sysinfo.os_name} ({sysinfo.os_version}, {sysinfo.os_bits}, {sysinfo.os_locale})");
            Logger.Information($"CPU: {sysinfo.cpu_name}, RAM: {sysinfo.ram_capacity}");
            Logger.Information($"GPU: {sysinfo.gpu_name} ({sysinfo.gpu_driver}, {sysinfo.gpu_ram})");
            Logger.Information($"HDD: {sysinfo.hdd_name}");
        }

        /// <summary>
        /// Global named mutex object
        /// </summary>
        private static Mutex launcher_mutex = null;

        /// <summary>
        /// String that identifies the named mutex.
        /// </summary>
        private static string mutex_name = "Local\\madalien.com_bnetlauncher_";
    }
}

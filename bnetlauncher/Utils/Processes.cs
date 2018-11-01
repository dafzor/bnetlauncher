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

namespace bnetlauncher.Utils
{
    // TODO: Make sure i don't need to check for users on these too
    static class Processes
    {
        /// <summary>
        /// Kill a process tree recursively
        /// </summary>
        /// <param name="process_id">Process ID.</param>
        public static void KillProcessAndChildsById(int process_id)
        {
            if (process_id == 0)
            {
                Logger.Warning("Attempted to kill child proccess of 0.");
                return;
            }

            using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE ParentProcessId = {process_id}"))
            {
                foreach (var result in searcher.Get())
                {
                    KillProcessAndChildsById(Convert.ToInt32(result["ProcessID"]));
                }
                try
                {
                    Process.GetProcessById(process_id).Kill();
                }
                catch (ArgumentException)
                {
                    // Process already exited.
                }
                catch (Exception ex)
                {
                    Logger.Information(ex.ToString());
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
        public static System.Diagnostics.ProcessStartInfo GetProcessStartInfoById(int process_id, int retry_count = 50)
        {
            var start_info = new System.Diagnostics.ProcessStartInfo();

            // IMPORTANT: If the game is slow to launch (computer just booted), it's possible that it will return a process ID but
            //            then fail to retrieve the start_info, thus we do this retry cycle to make sure we actually get the
            //            information we need.
            int retry = 1;
            bool done = false;
            while (retry < retry_count && done != true)
            {
                Logger.Information($"Attempt {retry} to find start parameters");
                try
                {
                    // IMPORTANT: We use System.Management API because Process.StartInfo is not populated if used on processes that we
                    //            didn't start with the Start() method. See additional information in Process.StartInfo documentation.
                    using (var searcher = new ManagementObjectSearcher($"SELECT CommandLine, ExecutablePath FROM Win32_Process WHERE ProcessId = {process_id}"))
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
                catch (Exception)
                {
                    Logger.Warning($"Failed attempt '{retry}'");
                }

                retry += 1;
                Thread.Sleep(100);
            }

            Logger.Information($"Filename:'{start_info.FileName}'.");
            Logger.Information($"Arguments:'{start_info.Arguments}'.");
            return start_info;
        }

        /// <summary>
        /// Returns the process id of the process with the given name that's closest to the date given.
        /// </summary>
        /// <param name="name">Name of the process to search for.</param>
        /// <param name="date">Date to filter from. Only processes with a greater then date will be returned.</param>
        /// <param name="timeout">time in seconds to retry looking for the process</param>
        /// <returns>Process Id of the process.</returns>
        public static int GetProcessByNameAfterDate(string name, DateTime date, int timeout = 15)
        {
            var game_process_date = DateTime.MaxValue;
            var stopwatch_timeout = new Stopwatch();

            int seconds_passed = 0;
            int game_process_id = 0;

            var wmiq = String.Format(
                "SELECT ProcessId, CreationDate FROM Win32_Process WHERE CreationDate > '{0}' AND Name LIKE '{1}%'",
                ManagementDateTimeConverter.ToDmtfDateTime(date).ToString(), name);

            using (var searcher = new ManagementObjectSearcher(wmiq))
            {
                stopwatch_timeout.Restart();


                // Keep looking until timeout is reached or we find a process
                while (game_process_id == 0 && stopwatch_timeout.Elapsed.TotalSeconds < timeout)
                {
                    // Avoids spamming the log with looking for process messages
                    if ((stopwatch_timeout.ElapsedMilliseconds - (seconds_passed*1000)) > 0)
                    {
                        Logger.Information($"Searching for process '{name}' for '{timeout - seconds_passed}' seconds.");
                        seconds_passed++;
                    }

                    foreach (var result in searcher.Get())
                    {
                        var result_process_id = Convert.ToInt32(result["ProcessId"]);
                        var result_process_date = ManagementDateTimeConverter.ToDateTime(result["CreationDate"].ToString());

                        Logger.Information($"Found game process started at '{result_process_date.ToString("hh:mm:ss.ffff")}' with pid:'{result_process_id}'");

                        // Closest to the given date is the one we return
                        if (result_process_date.Subtract(date).TotalMilliseconds < game_process_date.Subtract(date).TotalMilliseconds)
                        {
                            game_process_id = result_process_id;
                            game_process_date = result_process_date;
                        }
                    }
                }
            }
            return game_process_id;
        }
    }
}
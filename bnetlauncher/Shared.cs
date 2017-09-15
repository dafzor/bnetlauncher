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
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;

namespace bnetlauncher
{
    static class Shared
    {
        /// <summary>
        /// Path used to save the debug logs and client_lock_file
        /// </summary>
        public static string DataPath
        {
            get
            {
                return data_path;
            }
        }

        public static bool CreateDataPath()
        {
            try
            {
                // Creates data_path directory if it doesn't exist
                Directory.CreateDirectory(Shared.DataPath);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Logger function for debugging. It will output a line of text with current datetime in a log file per
        /// instance. The log file will only be written if a "enablelog" or "enablelog.txt" file exists in the
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
                Process.GetCurrentProcess().StartTime.ToString("yyyyMMdd_HHmmssffff") + ".log");

            StreamWriter file = new StreamWriter(log_file, append);
            file.WriteLine("[{0}]: {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"),
                line);
            file.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        public static FileVersionInfo VersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);

        /// <summary>
        /// Path used to save the debug logs and client_lock_file
        /// </summary>
        private static string data_path = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), VersionInfo.CompanyName, "bnetlauncher");
    }
}

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
using System.Diagnostics;
using System.IO;
using System.Management;
using bnetlauncher.Utils;

namespace bnetlauncher
{
    /// <summary>
    /// Defines common properties and methods for Client to allow adding aditional
    /// clients.
    /// </summary>
    abstract class Client
    {
        /// <summary>
        /// Unique Id that identifies the client in configuration files.
        /// </summary>
        public string Id { get; protected set; }

        /// <summary>
        /// Full Name of the client to display in UI and error messages.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Exe used to run the client and check to see if it's running.
        /// </summary>
        public string Exe { get; protected set; }

        /// <summary>
        /// Installation path were the client Exe is located.
        /// </summary>
        public abstract string InstallPath { get; }

        /// <summary>
        /// Flag that defines if the client is required to be running until the game
        /// is closed.
        /// </summary>
        public bool MustBeRunning { get; protected set; }

        /// <summary>
        /// Allows checking or setting if the client was started by bnetlauncher.
        /// It does so by using the checking, creating or deleting of a flag file.
        /// </summary>
        public bool WasStarted
        {
            get
            {
                return lockfile.Exists();
            }
            set
            {
                if (value == true && !lockfile.Exists())
                {
                    lockfile.Create();
                }
                if (value == false && lockfile.Exists())
                {
                    lockfile.Delete();
                }
            }
        }

        /// <summary>
        /// Returns if the client is installed based of the existance of it's executable.
        /// </summary>
        /// <returns></returns>
        public bool IsInstalled
        {
            get
            {
                // Checks if the battle.net client exe exists
                if (File.Exists(Path.Combine(InstallPath, Exe)))
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// True if the client is currently running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return GetProcessId() != 0;
            }
        }

        /// <summary>
        /// Returns the process Id of the currently running client instance.
        /// </summary>
        /// <returns>The process Id of the client.</returns>
        public int GetProcessId()
        {
            // TODO: What would happen if there's two clients running? Should we even care?
            // TODO: Now that helpers have the same exe name we can catch one by mistake...
            try
            {
                var current_user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_process WHERE Name = '{Exe}'"))
                {
                    // https://stackoverflow.com/questions/777548/how-do-i-determine-the-owner-of-a-process-in-c
                    foreach (ManagementObject result in searcher.Get())
                    {
                        // Returns the client instance owned by the current user.
                        // Trying to use a client running under a differnt user usually causes
                        // a second client instance to start.
                        var args = new string[] { string.Empty, string.Empty };
                        if (0 == Convert.ToInt32(result.InvokeMethod("GetOwner", args)))
                        {
                            if ($"{args[1]}\\{args[0]}" == current_user)
                            {
                                return Convert.ToInt32(result["ProcessId"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error finding client '{Id}' process id.", ex);
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        protected LockFile lockfile;


        public Client()
        {
            lockfile = new LockFile(this);
        }

        /// <summary>
        /// Closes the client.
        /// </summary>
        public virtual void Close()
        {
            Processes.KillProcessAndChildsById(GetProcessId());
            TrayArea.Refresh();
            lockfile.Delete();
        }

        /// <summary>
        /// Starts the client.
        /// </summary>
        /// <param name="create_lockfile">Flat to create lockfile to track if bnetlauncher started the client.</param>
        /// <returns>True if the process started.</returns>
        public virtual bool Start(bool create_lockfile = true)
        {
            var client = Process.Start(Path.Combine(InstallPath, Exe));

            // Waits until it goes idle
            var ret = client.WaitForInputIdle();
            Logger.Information($"Client '{Id}' running with pid:'{GetProcessId()}'.");

            // create the started file that signals the client was started by us.
            if (create_lockfile)
            {
                lockfile.Create();
            }
            return ret;
        }

        /// <summary>
        /// Launches a game with the given client specific command.
        /// Each client will tipicly override this to acomodate teh specificities.
        /// </summary>
        /// <param name="cmd">cmd used to start the game.</param>
        public virtual bool Launch(string cmd)
        {
            try
            {
                Process.Start(cmd);

                // If bnetlauncher started the client and it needs to be running for
                // for the game to work we add the current bnetlauncher process id
                // to the lock file.
                if (MustBeRunning && WasStarted)
                {
                    lockfile.AppendPid();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while starting '{Id}'.", ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Shows the main window of the Launcher
        /// </summary>
        public virtual void ShowWindow()
        {
            var client_pid = GetProcessId();

            if (client_pid == 0)
            {
                Logger.Warning($"Attempted to open {Id} Window without it running.");
                return;
            }

            Logger.Information($"Openting {Id} Window and waiting 3 seconds.");
            Process.Start(Path.Combine(InstallPath, Exe));
        }

        /// <summary>
        /// Nested class that's used to keep track if bnetlauncher started the client
        /// </summary>
        protected class LockFile
        {
            /// <summary>
            /// Access to the parent Client members.
            /// </summary>
            private readonly Client client;

            /// <summary>
            /// Complete file path to the lock file.
            /// </summary>
            public string FileName
            {
                get
                {
                    return Path.Combine(Program.DataPath, $"{client.Id}.lock");
                }
            }

            public LockFile(Client client)
            {
                this.client = client;
            }


            /// <summary>
            /// Creates a file signaling that the client was started by bnetlauncher.
            /// </summary>
            public void Create()
            {

                // We explicitly call close on the file we just created so that when we try to delete the file 
                // it's not locked causing the next launch to also trigger a close of the client.
                File.Create(FileName).Close();
            }

            /// <summary>
            /// Deletes the lock file.
            /// </summary>
            public void Delete()
            {
                File.Delete(FileName);
            }

            public bool Exists()
            {
                return File.Exists(FileName);
            }

            public void AppendPid()
            {
               
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading;

namespace bnetlauncher
{
    abstract class Client
    {
        /// <summary>
        /// Id that identifies the client in the gamedb
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// Name of the client for error messages.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The exe of the client
        /// </summary>
        public string Exe { get; internal set; }

        /// <summary>
        /// Installation path were the client Exe is located
        /// </summary>
        public abstract string InstallPath { get; }

        /// <summary>
        /// Returns true if the client was started by bnetlauncher by using the existance of a flag file.
        /// </summary>
        public bool WasStarted
        {
            get
            {
                // Checks if the battle.net client exe exists
                if (File.Exists(System.IO.Path.Combine(Shared.DataPath, $"{Id}.started")))
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns if the client is instaled based of the existance of it's executable.
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
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT ProcessId FROM Win32_process WHERE Name = '{Exe}'"))
                {
                    foreach (var result in searcher.Get())
                    {
                        return Convert.ToInt32(result["ProcessId"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Shared.Logger($"Error finding {Id} client pid. {ex.ToString()}");
            }
            return 0;
        }

        /// <summary>
        /// Closes the client if client.started file exists it's deleted.
        /// </summary>
        public void Close()
        {
            Program.KillProcessAndChildren(GetProcessId());
            File.Delete(Path.Combine(Shared.DataPath, $"{Id}.started"));   
        }

        /// <summary>
        /// Generic Client starting code
        /// </summary>
        /// <returns>True if the process started.</returns>
        public abstract bool Start();
        //{
        //    var client = Process.Start(Path.Combine(InstallPath, Exe));

        //    // Waits until it goes idle
        //    var ret = client.WaitForInputIdle();

        //    // create the started file that signals the client was started by us.
        //    File.Create(Path.Combine(Shared.DataPath, $"{Id}.started"));
        //    return ret;
        //}

        /// <summary>
        /// Launches a game with the given client specific command.
        /// Each client will tipicly override this to acomodate teh specificities.
        /// </summary>
        /// <param name="cmd">cmd used to start the game.</param>
        public abstract bool Launch(string cmd);
        //{
        //    try
        //    {
        //        Process.Start(cmd);
        //    }
        //    catch (Exception ex)
        //    {
        //        Shared.Logger(ex.ToString());
        //        return false;
        //    }
        //    return true;
        //}
    }
}

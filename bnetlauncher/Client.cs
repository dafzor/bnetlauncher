using System;
using System.Diagnostics;
using System.IO;
using System.Management;

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
        /// Returns true if the client was started by bnetlauncher.
        /// It does so by using the existance of a flag file.
        /// </summary>
        public bool WasStarted
        {
            get
            {
                return lockfile.Exists();
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
            Program.KillProcessAndChildren(GetProcessId());
            lockfile.Delete();
        }

        /// <summary>
        /// Starts the client.
        /// </summary>
        /// <returns>True if the process started.</returns>
        public virtual bool Start()
        {
            var client = Process.Start(Path.Combine(InstallPath, Exe));

            // Waits until it goes idle
            var ret = client.WaitForInputIdle();

            // create the started file that signals the client was started by us.
            lockfile.Create();
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
                Shared.Logger(ex.ToString());
                return false;
            }
            return true;
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
            private string lock_file;

            public LockFile(Client client)
            {
                this.client = client;
                lock_file = Path.Combine(Shared.DataPath, $"{client.Id}.lock");
            }

            /// <summary>
            /// Creates a file signaling that the client was started by bnetlauncher.
            /// </summary>
            public void Create()
            {
                // We explicitly call close on the file we just created so that when we try to delete the file 
                // it's not locked causing the next launch to also trigger a close of the client.
                File.Create(lock_file).Close();
            }

            /// <summary>
            /// 
            /// </summary>
            public void Delete()
            {
                File.Delete(lock_file);
            }

            public bool Exists()
            {
                return File.Exists(lock_file);
            }

            public void AppendPid()
            {
               
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace bnetlauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            string bnet_cmd = "battlenet://";

            if (args.Length > 0)
            {
                bnet_cmd += args[0].Trim();
            }
            else
            {
                string message = "Use one of the following **case sensitive** parameters to launch the game:\n" +
                    "WoW\t= World of Warcraft\n" +
                    "D3\t= Diablo 3\n" +
                    "WTCG\t= Heartstone\n" +
                    "Pro\t= Overwatch\n" +
                    "S2\t= Starcraft 2\n" +
                    "Hero\t= Heroes of the Storm\n";

                Application.EnableVisualStyles();
                MessageBox.Show(message, "Howto Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            // is b.net open? launch b.net with parent being explorer
            if (Process.GetProcessesByName("Battle.net").Length <= 0)
            {
                //ProcessCreator.CreateProcess(Properties.Settings.Default.BnetPath, 0);
            }


            // fire up game trough b.net
            //Process.Start(bnet_cmd);

            /*
            // finds the b.net process, and see's what was the new child process that was started
            try
            {
                Process bnet_process = Process.GetProcessesByName("Battle.Net")[0];
            }
            catch (IndexOutOfRangeException ex)
            {
                MessageBox.Show("Can't find b.net");
            }

            ;

            /*
            if (bnet_processes.Length > 0)
            {
                int parent_id = bnet_processes[0].Id;
                List<Process> bnet_childs = GetChildProcesses(parent_id);
                Process game;
                foreach (var child in bnet_childs)
                {
                    if (child.Id > parent_id && child.ProcessName
                }
            }

            //Process[] bnet_childs = bnet_process[0].Id

            foreach(var bnet_p in bnet_processes)
            {

            }

            // is b.net already running? if not start it without allowing steam to track ittask
            Process[] processes = Process.GetProcessesByName("Battle.Net");
            if (processes.Length <= 0)
            {
                Process stub = new Process();
                //stub.StartInfo.FileName = Application.ExecutablePath;
                stub.StartInfo.FileName = "C:\\Program Files (x86)\\Battle.net\\Battle.net Launcher.exe";
                //stub.StartInfo.Arguments = "bnet";
                stub.StartInfo.UseShellExecute = false;

                stub.StartInfo.

                stub.Start();
                //stub.WaitForExit();
                //Thread.Sleep(10000);
            }

            // start game
            Thread.Sleep(10000);
            Process.Start(cmd);
            */
        }


        public static List<Process> GetChildProcesses(int process_id)
        {
            var results = new List<Process>();

            // query the management system objects for any process that has the current
            // process listed as it's parentprocessid
            string queryText = string.Format("select processid from win32_process where parentprocessid = {0}", process_id);
            using (var searcher = new ManagementObjectSearcher(queryText))
            {
                foreach (var obj in searcher.Get())
                {
                    object data = obj.Properties["processid"].Value;
                    if (data != null)
                    {
                        // retrieve the process
                        var childId = Convert.ToInt32(data);
                        var childProcess = Process.GetProcessById(childId);

                        // ensure the current process is still live
                        if (childProcess != null)
                            results.Add(childProcess);
                    }
                }
            }
            return results;
        }
    }
}

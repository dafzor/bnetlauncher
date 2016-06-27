using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace bnetlauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            //CloseBnetProcess();
            //return;

            // Parse paramenters
            string bnet_cmd = "battlenet://";
            if (args.Length > 0)
            {
                bnet_cmd += args[0].Trim();
            }
            else
            {
                string message = "Use one of the following *case sensitive* parameters to launch the game:\n" +
                    "WoW\t= World of Warcraft\n" +
                    "D3\t= Diablo 3\n" +
                    "WTCG\t= Heartstone\n" +
                    "Pro\t= Overwatch\n" +
                    "S2\t= Starcraft 2\n" +
                    "Hero\t= Heroes of the Storm\n";

                MessageBox.Show(message, "Howto Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // flag to close the client on exit or not.
            bool bnet_close = false;

            // is b.net open? If not open it up and mark it for termination on exit
            // TODO: Find a way to start b.net launcher without steam attaching overlay
            if (GetBnetProcessId() == 0)
            {
                Process.Start("battlenet://");
                bnet_close = true;
            }

            // HACK: b.net launcher starts two helper child process, until they're running it will ignore direct launch commands
            // so we wait for both process to start before continuing.
            while (Process.GetProcessesByName("Battle.net Helper").Length < 2)
            {
                Thread.Sleep(100);
            }


            // fire up game trough b.net
            DateTime client_start_date = DateTime.Now;
            Process.Start(bnet_cmd);

            // Waits for the client to start trough battle.net
            var game_process_id = 0;
            do
            {
                game_process_id = GetLastBnetProcessIdSinceDate(client_start_date);
            } while (game_process_id == 0);


            // copies the game proces arguments
            Process process = new Process();
            process.StartInfo = GetProcessStartInfoById(game_process_id);

            // kills the client that's child to the launcher and starts a new one that's child to us
            Process.GetProcessById(game_process_id).Kill();
            process.Start();

            // close client if it wasn't started
            if (bnet_close)
            {
                Process.GetProcessById(GetBnetProcessId()).Kill();
            }
        }


        private static ProcessStartInfo GetProcessStartInfoById(int process_id)
        {
            var start_info = new ProcessStartInfo();

            using (var searcher = new ManagementObjectSearcher("SELECT CommandLine, ExecutablePath FROM Win32_Process WHERE ProcessId = " +
                process_id.ToString()))
            {
                foreach (var result in searcher.Get())
                {
                    start_info.FileName = result["ExecutablePath"].ToString();

                    var command_line = result["CommandLine"].ToString();
                    var cut_off = start_info.FileName.Length;

                    if (command_line[0] == '"')
                    {
                        cut_off += 2;
                    }
                    start_info.Arguments = command_line.Substring(cut_off);
                    break;
                }
            }
            return start_info;
        }

        private static int GetLastBnetProcessIdSinceDate(DateTime date)
        {

            var last_process_id = 0;
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_Process WHERE " +
                "CreationDate > '" + ManagementDateTimeConverter.ToDmtfDateTime(date).ToString() + "' AND " +
                "Name <> 'Battle.net Helper.exe' AND " +
                "ParentProcessId = " + GetBnetProcessId()))
            {
                foreach (var result in searcher.Get())
                {
                    var result_process_id = Convert.ToInt32(result["ProcessId"]);

                    if (result_process_id > last_process_id)
                    {
                        last_process_id = result_process_id;
                    }
                }
            }
            return last_process_id;
        }

        private static int GetBnetProcessId()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_process WHERE Name = 'Battle.Net.exe'"))
            {
                foreach (var result in searcher.Get())
                {
                    return Convert.ToInt32(result["ProcessId"]);
                }
            }

            return 0;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpClassName, string lpWindowName);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 wMsg, IntPtr wParam, IntPtr lParam);
        private const UInt32 WM_MOUSEMOVE = 0x0200;

        private static void CloseBnetProcess()
        {
            var bnet_process = Process.GetProcessById(GetBnetProcessId());
            bnet_process.Kill();

            var tray_handle = FindWindowEx(FindWindow("Shell_TrayWnd", ""), IntPtr.Zero, "TrayNotifyWnd", null);
            var pager_handle = FindWindowEx(tray_handle, IntPtr.Zero, "SysPager", "");
            var area_handle = FindWindowEx(pager_handle, IntPtr.Zero, "", "Notification Area");
            SendMessage(area_handle, WM_MOUSEMOVE, (IntPtr)0, (IntPtr)0);

        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace bnetlauncher.Utils
{
    public static class Logger
    {
        public static bool OutputToFile { get; set; }
        public static bool OutPutToConsole { get; set; }

        static Logger()
        {
            OutPutToConsole = Environment.UserInteractive;
            OutputToFile = true;
        }

        public static void Information(string message,
            [CallerFilePath] string src_path = "", [CallerMemberName] string src_member = "", [CallerLineNumber] int src_line = 0)
        {
            Message(message, null, src_path, src_member, src_line, "info");
        }

        public static void Warning(string message, Exception ex = null,
            [CallerFilePath] string src_path = "", [CallerMemberName] string src_member = "", [CallerLineNumber] int src_line = 0)
        {
            Message(message, ex, src_path, src_member, src_line, "warn");

        }

        public static void Error(string message, Exception ex = null,
            [CallerFilePath] string src_path = "", [CallerMemberName] string src_member = "", [CallerLineNumber] int src_line = 0)
        {
            Message(message, ex, src_path, src_member, src_line, "error");
        }


        private static void Message(string message, Exception ex, string src_path, string src_member,int src_line, string type = "inf")
        {
            var line = $"{DateTime.Now.ToString("HH:mm:ss.ffff")}|{Process.GetCurrentProcess().Id}|{GetSrcFile(src_path)}.{src_member}:{src_line}|{type}|{message}";

            if (OutputToFile)
            {
                WriteLog(line);
                if (ex != null)
                {
                    WriteLog(ex.ToString());
                }
            }
            if (OutPutToConsole)
            {
                Console.WriteLine(line);
                if (ex != null)
                {
                    Console.Write(ex.ToString());
                }
            }

        }

        private static void WriteLog(string line)
        {
            if (!File.Exists(Path.Combine(Program.DataPath, "enablelog")) &&
                !File.Exists(Path.Combine(Program.DataPath, "enablelog.txt")) &&
                !File.Exists(Path.Combine(Program.DataPath, "enablelog.txt.txt")))
            {
                // only enable logging if a file named enablelog exists in 
                return;
            }

            var log_file = Path.Combine(Program.DataPath, "debug_" +
                Process.GetCurrentProcess().StartTime.ToString("yyyyMMdd") + ".log");


            StreamWriter file = new StreamWriter(log_file, true);
            file.WriteLine(line);
            file.Close();
        }

        private static string GetSrcFile(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// TODO:
/// - support multiple programs writing to the file at the same time
/// - make it so that blocked text file doesn't halt execution
/// - add old log cleanup/trimming code
/// - make code more robust?
/// - something i'm probably forgeting
/// </summary>

namespace bnetlauncher.Utils
{
    public static class Logger
    {
        public static bool OutputToFile { get; set; }
        public static bool OutPutToConsole { get; set; }

        private static readonly string log_file = Path.Combine(Program.DataPath, $"debug_{Process.GetCurrentProcess().StartTime.ToString("yyyyMMdd")}.log");

        static Logger()
        {
            OutPutToConsole = Environment.UserInteractive;
            OutputToFile = File.Exists(Path.Combine(Program.DataPath, "enablelog")) ||
                File.Exists(Path.Combine(Program.DataPath, "enablelog.txt")) ||
                File.Exists(Path.Combine(Program.DataPath, "enablelog.txt.txt"));
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
                ConsoleWrite(message, ex, src_path, src_member, src_line, type);
            }
        }

        private static void WriteLog(string line)
        {
            StreamWriter file = new StreamWriter(log_file, true);
            file.WriteLine(line);
            file.Close();
        }

        private static void ConsoleWrite(string message, Exception ex, string src_path, string src_member, int src_line, string type)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(DateTime.Now.ToString("HH:mm:ss.ffff"));

            Console.ResetColor(); Console.Write("|");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(Process.GetCurrentProcess().Id);

            Console.ResetColor(); Console.Write("|");

            switch (type)
            {
                case "error":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "warn":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
            }

            Console.Write($"{GetSrcFile(src_path)}.{src_member}:{src_line}|{type}");

            Console.ResetColor(); Console.Write("|");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);

            if (ex != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(ex.ToString());
            }

            Console.ResetColor();
        }

        private static string GetSrcFile(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}

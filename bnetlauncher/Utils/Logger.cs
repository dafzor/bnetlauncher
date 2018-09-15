using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// TODO:
/// - support multiple programs writing to the file at the same time
/// - make it so that blocked text file doesn't halt execution https://stackoverflow.com/questions/29962885/writing-to-a-file-asynchronously-but-in-order
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

        /// <summary>
        /// Read only structure contained all the information for a single log entry
        /// </summary>
        private struct LogEntry
        {
            public readonly string message;
            public readonly Exception ex;
            public readonly string src_path;
            public readonly string src_member;
            public readonly int src_line;
            public readonly string type;

            public LogEntry(string message, Exception ex, string src_path, string src_member, int src_line, string type = "inf")
            {
                this.message = message;
                this.ex = ex;
                this.src_path = src_path;
                this.src_member = src_member;
                this.src_line = src_line;
                this.type = type;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static readonly string log_file = Path.Combine(Program.DataPath, $"debug_{Process.GetCurrentProcess().StartTime.ToString("yyyyMMdd")}.log");

        /// <summary>
        /// Log Entry queues
        /// </summary>
        private static BlockingCollection<LogEntry> log_queue = new BlockingCollection<LogEntry>();

        static Logger()
        {
            OutPutToConsole = Environment.UserInteractive;
            OutputToFile = File.Exists(Path.Combine(Program.DataPath, "enablelog")) ||
                File.Exists(Path.Combine(Program.DataPath, "enablelog.txt")) ||
                File.Exists(Path.Combine(Program.DataPath, "enablelog.txt.txt"));

            // creates the worker thread as background so it exits when main program finishes
            var logger_worker = new Thread(LoggerWorker);
            logger_worker.IsBackground = true;
            logger_worker.Start();
        }

        public static void Information(string message,
            [CallerFilePath] string src_path = "", [CallerMemberName] string src_member = "", [CallerLineNumber] int src_line = 0)
        {
            log_queue.Add(new LogEntry(message, null, src_path, src_member, src_line, "INFO"));
        }

        public static void Warning(string message, Exception ex = null,
            [CallerFilePath] string src_path = "", [CallerMemberName] string src_member = "", [CallerLineNumber] int src_line = 0)
        {
            log_queue.Add(new LogEntry(message, ex, src_path, src_member, src_line, "WARN"));

        }

        public static void Error(string message, Exception ex = null,
            [CallerFilePath] string src_path = "", [CallerMemberName] string src_member = "", [CallerLineNumber] int src_line = 0)
        {
            log_queue.Add(new LogEntry(message, ex, src_path, src_member, src_line, "ERROR"));
        }

        private static void LoggerWorker()
        {
            while (!log_queue.IsCompleted)
            {
                try
                {
                    // Tries to take another message from the queue
                    // and blocks if the queue is empty.
                    var msg = log_queue.Take();

                    if (OutPutToConsole)
                    {
                        ConsoleWrite(msg);
                    }
                    if (OutputToFile)
                    {
                        FileWrite(msg);
                    }
                }
                catch (InvalidOperationException) { }
            }
        }

        private static void FileWrite(LogEntry le)
        {
            var line = $"{DateTime.Now.ToString("HH:mm:ss.ffff")}|{Process.GetCurrentProcess().Id}|{GetSrcFile(le.src_path)}.{le.src_member}:{le.src_line}|{le.type}|{le.message}";

            while (true)
            {
                try
                {
                    using (var file = new StreamWriter(log_file, true))
                    {
                        file.WriteLine(line);
                        if (le.ex != null)
                        {
                            file.WriteLine(le.ex.ToString());
                        }
                    }
                    return; // if we reach here we're done
                }
                // failed to get the file so try again
                catch (IOException) { }
            }
        }

        private static void ConsoleWrite(LogEntry le)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(DateTime.Now.ToString("HH:mm:ss.ffff"));

            Console.ResetColor(); Console.Write("|");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(Process.GetCurrentProcess().Id);

            Console.ResetColor(); Console.Write("|");

            switch (le.type)
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

            Console.Write($"{GetSrcFile(le.src_path)}.{le.src_member}:{le.src_line}|{le.type}");

            Console.ResetColor(); Console.Write("|");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(le.message);

            if (le.ex != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(le.ex.ToString());
            }

            Console.ResetColor();
        }

        private static string GetSrcFile(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}

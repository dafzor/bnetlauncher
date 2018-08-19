using System;
using Microsoft.Win32.TaskScheduler;

namespace bnetlauncher.Utils
{
    /// TODO:
    /// - restrict all operations to bnetlauncher folder in tasks
    /// - check for permission access on all operations
    /// - properly implement create and delete

    /// <summary>
    /// Provides methods to create and run scheduled tasks to start clients.
    /// This allows the client to start unattached to steam (no overlay)
    /// allowing it to be left running without affecting ingame status in steam.
    /// </summary>
    public static class Tasks
    {
        private static TaskService tservice;

        static Tasks()
        {
            tservice = new TaskService();
        }

        public static void Create(string name, string exe)
        {
            //var ti = tservice.AddTask($"bnetlauncher\\{name}", Trigger.CreateTrigger(TaskTriggerType., $"\"{exe}\"");
            //create task with no trigger
        }

        public static void Delete(string name)
        {
            //tservice.RootFolder.DeleteTask(name);
        }

        public static void Run(string name)
        {
            tservice.FindTask(name).Run();
        }

        public static bool Exists(string name)
        {
            if (tservice.FindTask(name) != null)
            {
                return true;
            }
            return false;
        }
    }
}
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
using Microsoft.Win32.TaskScheduler;

namespace bnetlauncher.Utils
{
    // https://stackoverflow.com/questions/3977801/c-sharp-api-for-task-scheduler-2-0

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
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
using System.Reflection;
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
        private static TaskService service;
        private static TaskFolder folder;

        private static FileVersionInfo VersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        static Tasks()
        {
            service = new TaskService();

            // make sure the folder exists and creates it if it doesn't
            if (null == (folder = service.GetFolder(VersionInfo.FileDescription)))
            {
                service.RootFolder.CreateFolder(VersionInfo.FileDescription);
                folder = service.GetFolder(VersionInfo.FileDescription);
            }
        }

        public static Task Create(string name, string cmd)
        {
            TaskDefinition td = service.NewTask();

            td.Principal.LogonType = TaskLogonType.InteractiveToken;
            td.RegistrationInfo.Description = $"{VersionInfo.FileMajorPart}.{VersionInfo.FileMinorPart}";

            td.Settings.AllowDemandStart = true;
            td.Settings.IdleSettings.StopOnIdleEnd = false;
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.StopIfGoingOnBatteries = false;

            td.Actions.Add(new ExecAction(cmd));

            Logger.Information($"Creating task for {name}.");
            return folder.RegisterTaskDefinition(name, td);

        }

        public static void Delete(string name)
        {
            folder.DeleteTask(name);
        }

        public static void Run(string name)
        {
            // Is this really necessary?
            var tasks = folder.Tasks;

            foreach (var task in tasks)
            {
                if (task.Name == name)
                {
                    Logger.Information($"Starting {name} task.");
                    task.Run();
                    return;
                }
            }
        }

        public static bool Exists(string name)
        {
            var tasks = folder.Tasks;

            foreach (var task in tasks)
            {
                if (task.Name == name)
                {
                    try
                    {
                        var current_version = new Version($"{VersionInfo.FileMajorPart}.{VersionInfo.FileMinorPart}");
                        var task_version = new Version(task.Definition.RegistrationInfo.Description);

                        if (task_version < current_version)
                        {
                            // the task version is newer or the same as the one we have
                            Logger.Information($"Found outdated task for {name} v{task_version}, current is v{current_version}");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error comparing task versions", ex);
                    }

                    // doesn't exist or older version
                    Logger.Information($"Found task for {name}.");
                    return true;
                }
            }
            // didn't find a matching task
            Logger.Information($"No task for {name}.");
            return false;
        }

        public static bool CreateAndRun(string name, string cmd)
        {
            if (!Exists(name))
            {
                if (null == Create(name, cmd))
                {
                    Logger.Warning($"Failed to create task for {name}.");
                    return false;
                }
            }

            Run(name);
            return true;
        }
    }
}
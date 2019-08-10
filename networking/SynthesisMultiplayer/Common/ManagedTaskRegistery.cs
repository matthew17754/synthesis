using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using TaskEntry =
    SynthesisMultiplayer.Util.Either<
        (
            SynthesisMultiplayer.Threading.IManagedTask,
            System.Threading.Tasks.Task
        ),
        System.Guid>;
namespace SynthesisMultiplayer.Common
{
    internal class ManagedTaskRegistry
    {
        Dictionary<Guid, TaskEntry> Tasks;
        Dictionary<string, Guid> TaskNames;
        Mutex TaskLock;
        private ManagedTaskRegistry()
        {
            Tasks = new Dictionary<Guid, TaskEntry>();
            TaskNames = new Dictionary<string, Guid>();
            TaskLock = new Mutex();
        }
        public static ManagedTaskRegistry Instance { get { return Internal.instance; } }
        private class Internal
        {
            static Internal() { }
            internal static readonly ManagedTaskRegistry instance = new ManagedTaskRegistry();
        }

        public static (IManagedTask, Task) GetTask(Guid taskId)
        {
            return getTaskImpl(taskId);
        }
        public static (IManagedTask, Task) GetTask(string taskName)
        {
            lock (Instance.TaskLock)
            {
                if (!Instance.TaskNames.ContainsKey(taskName))
                {
                    return (null, null);
                }
                return getTaskImpl(Instance.TaskNames[taskName]);
            }
        }

        public static Task GetProcess(Guid taskId)
        {
            return getTaskImpl(taskId).Left().Item2;
        }
        public static Task GetProcess(string taskName)
        {
            lock (Instance.TaskLock)
            {
                if (!Instance.TaskNames.ContainsKey(taskName))
                {
                    return null;
                }
                return getTaskImpl(Instance.TaskNames[taskName]).Left().Item2;
            }
        }
        public static IManagedTask GetTaskObject(Guid taskId)
        {
            return getTaskImpl(taskId).Left().Item1;
        }
        public static IManagedTask GetTaskObject(string taskName)
        {
            lock (Instance.TaskLock)
            {
                if (!Instance.TaskNames.ContainsKey(taskName))
                {
                    return null;
                }
                return getTaskImpl(Instance.TaskNames[taskName]).Left().Item1;
            }
        }
        // Recurses through 
        private static TaskEntry getTaskImpl(Guid taskId)
        {
            lock (Instance.TaskLock)
            {
                if (!Instance.Tasks.ContainsKey(taskId))
                {
                    return null;
                }
                var taskData = Instance.Tasks[taskId];
                if (taskData.GetState() == TaskEntry.State.Invalid)
                {
                    return null;
                }
                if (taskData.GetState() == TaskEntry.State.Left)
                {
                    return taskData;
                }
                return getTaskImpl(taskData);
            }
        }

        public static Guid StartTask(IManagedTask taskInstance, string name = null, ITaskContext context = null)
        {
            if (taskInstance.GetStatus() != ManagedTaskStatus.Created)
            {
                throw new Exception("Cannot start a task that is already running. Do not call OnStart or spawn tasks directly.");
            }
            context = context ?? new TaskContext();
            var task = ManagedTaskHelper.Run(taskInstance, context);
            var taskId = Guid.NewGuid();
            lock (Instance.TaskLock)
            {
                if (Instance.Tasks.ContainsKey(taskId))
                {
                    throw new Exception("Task with id '" + taskId.ToString() + "' already registered.");
                }
                if (name != null && Instance.TaskNames.ContainsKey(name))
                {
                    throw new Exception("Task with name '" + name + "' already registered.");
                }
                Instance.Tasks[taskId] = new TaskEntry((taskInstance, task));
                if (name != null)
                    Instance.TaskNames[name] = taskId;
            }
            return taskId;
        }

        public static void RestartTask(Guid taskId, bool doRestoreState = false)
        {
            var (task, process) = GetTask(taskId);
            Dictionary<string, dynamic> state = null;
            task.Cancel();
            process.Wait();
            task.Dispose();
            if (doRestoreState)
                state = task.DumpState(new Dictionary<string, dynamic>());
            process.Dispose();
            process = ManagedTaskHelper.Run(task, new TaskContext());
            lock(Instance.TaskLock)
            {
                Instance.Tasks[taskId] = new Either<(IManagedTask, Task), Guid>((task, process));
            }
        }
    }
}

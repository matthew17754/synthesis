using SynthesisMultiplayer.Threading.Execution;
using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using TaskEntry =
    SynthesisMultiplayer.Util.Either<
        (
            SynthesisMultiplayer.Threading.Execution.IManagedTask Task,
            System.Threading.Tasks.Task Process,
            SynthesisMultiplayer.Util.Channel<(string, SynthesisMultiplayer.Threading.Execution.AsyncCallHandle)> Channel
        ),
        System.Guid>;
using MessageChannel = SynthesisMultiplayer.Util.Channel<(string, SynthesisMultiplayer.Threading.Execution.AsyncCallHandle)>;
namespace SynthesisMultiplayer.Threading.Execution
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
            var taskInfo = getTaskImpl(taskId);
            return (taskInfo.Left.Task, taskInfo.Left.Process);
        }
        public static (IManagedTask, Task) GetTask(string taskName)
        {
            lock (Instance.TaskLock)
            {
                if (!Instance.TaskNames.ContainsKey(taskName))
                {
                    return (null, null);
                }
                var taskInfo = getTaskImpl(Instance.TaskNames[taskName]);
                return (taskInfo.Left.Task, taskInfo.Left.Process);
            }
        }

        public static Task GetProcess(Guid taskId)
        {
            return getTaskImpl(taskId).Left.Process;
        }
        public static Task GetProcess(string taskName)
        {
            lock (Instance.TaskLock)
            {
                if (!Instance.TaskNames.ContainsKey(taskName))
                {
                    return null;
                }
                return getTaskImpl(Instance.TaskNames[taskName]).Left.Process;
            }
        }
        public static IManagedTask GetTaskObject(Guid taskId)
        {
            return getTaskImpl(taskId).Left.Task;
        }
        public static IManagedTask GetTaskObject(string taskName)
        {
            lock (Instance.TaskLock)
            {
                if (!Instance.TaskNames.ContainsKey(taskName))
                {
                    return null;
                }
                return getTaskImpl(Instance.TaskNames[taskName]).Left.Task;
            }
        }

        public static MessageChannel GetChannel(Guid taskId)
        {
            return getTaskImpl(taskId).Left.Channel;
        }
        public static MessageChannel GetChannel(string taskName)
        {
            lock (Instance.TaskLock)
            {
                if (!Instance.TaskNames.ContainsKey(taskName))
                {
                    return null;
                }
                return getTaskImpl(Instance.TaskNames[taskName]).Left.Channel;
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
            if (taskInstance.Status != ManagedTaskStatus.Created)
            {
                throw new Exception("Cannot start a task that is already running. Do not call OnStart or spawn tasks directly.");
            }
            var taskId = Guid.NewGuid();
            context = context ?? new TaskContext();
            var channel = new MessageChannel();
            var task = taskInstance.Run(taskId, channel, context);
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
                Instance.Tasks[taskId] = new TaskEntry((taskInstance, task, channel));
                if (name != null)
                    Instance.TaskNames[name] = taskId;
            }
            return taskId;
        }

        public static void RestartTask(Guid taskId, bool doRestoreState = false)
        {
            var (task, process) = GetTask(taskId);
            var messageChannel = GetChannel(taskId);
            Dictionary<string, dynamic> state = null;
            task.Terminate();
            process.Wait();
            if (doRestoreState)
                state = StateBackup.DumpState(task);
            process.Dispose();
            process = task.Run(taskId, messageChannel, context: new TaskContext(), state: state);
            lock (Instance.TaskLock)
            {
                Instance.Tasks[taskId] = new TaskEntry((task, process, messageChannel));
            }
        }

        public static void RestartTask(string taskName, bool doRestoreState = false)
        {
            var (task, process) = GetTask(taskName);
            var messageChannel = GetChannel(taskName);
            Dictionary<string, dynamic> state = null;
            task.Terminate();
            process.Wait();
            task.Terminate();
            if (doRestoreState)
                state = StateBackup.DumpState(task);
            process.Dispose();
            process = task.Run(GetTaskObject(taskName).Id, messageChannel, context: new TaskContext(), state: state);
            lock (Instance.TaskLock)
            {
                Instance.Tasks[task.Id] = new TaskEntry((task, process, messageChannel));
            }
        }

        public static void TerminateTask(Guid taskId, params dynamic[] args)
        {
            lock (Instance.TaskLock)
            {
                var (task, proc) = GetTask(taskId);
                task.Terminate(args: args);
                GetChannel(taskId).Close();
            }
        }

        public static void TerminateTask(string taskName, params dynamic[] args)
        {
            lock (Instance.TaskLock)
            {
                var (task, proc) = GetTask(taskName);
                task.Terminate(args: args);
                GetChannel(taskName).Close();
            }
        }
        public static void Send(Guid taskId, (string, AsyncCallHandle) message)
        {
            if (GetTask(taskId).Item1 == null || GetTask(taskId).Item2 == null)
            {
                throw new Exception("No task found with id '" + taskId.ToString() + "'");
            }
            GetChannel(taskId).Send(message);
        }
        public static void Send(string taskName, (string, AsyncCallHandle) message)
        {
            if (GetTask(taskName).Item1 == null || GetTask(taskName).Item2 == null)
            {
                throw new Exception("No task found with name '" + taskName + "'");
            }
            GetChannel(taskName).Send(message);
        }
    }
}

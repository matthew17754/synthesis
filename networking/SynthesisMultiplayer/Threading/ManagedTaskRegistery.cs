using Multiplayer.Threading.Runtime;
using Multiplayer.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using TaskEntry =
    Multiplayer.Util.Either<
        (
            Multiplayer.Threading.IManagedTask Task,
            System.Threading.Thread Process,
            Multiplayer.Util.Channel<(string, Multiplayer.Threading.Runtime.AsyncCallHandle)> Channel
        ),
        System.Guid>;
using MessageChannel = Multiplayer.Util.Channel<(string, Multiplayer.Threading.Runtime.AsyncCallHandle)>;
namespace Multiplayer.Threading
{
    internal class ManagedTaskRegistry
    {
        Dictionary<Guid, TaskEntry> Tasks;
        Dictionary<string, Guid> TaskNames;
        ReaderWriterLockSlim TaskLock;
        private ManagedTaskRegistry()
        {
            Tasks = new Dictionary<Guid, TaskEntry>();
            TaskNames = new Dictionary<string, Guid>();
            TaskLock = new ReaderWriterLockSlim();
        }

        public static ManagedTaskRegistry Instance { get { return Internal.instance; } }
        private class Internal
        {
            static Internal() { }
            internal static readonly ManagedTaskRegistry instance = new ManagedTaskRegistry();
        }

        public static (IManagedTask, Thread) GetTask(Guid taskId)
        {
            var taskInfo = getTaskImpl(taskId);
            return (taskInfo.Left.Task, taskInfo.Left.Process);
        }
        public static (IManagedTask, Thread) GetTask(string taskName)
        {
            Instance.TaskLock.EnterReadLock();
            if (!Instance.TaskNames.ContainsKey(taskName))
            {
                Instance.TaskLock.ExitReadLock();
                return (null, null);
            }
            Instance.TaskLock.ExitReadLock();
            var taskInfo = getTaskImpl(Instance.TaskNames[taskName]);
            return (taskInfo.Left.Task, taskInfo.Left.Process);
        }

        public static Thread GetProcess(Guid taskId)
        {
            return getTaskImpl(taskId).Left.Process;
        }
        public static Thread GetProcess(string taskName)
        {
            Instance.TaskLock.EnterReadLock();
            if (!Instance.TaskNames.ContainsKey(taskName))
            {
                Instance.TaskLock.ExitReadLock();
                return null;
            }
            Instance.TaskLock.ExitReadLock();
            return getTaskImpl(Instance.TaskNames[taskName]).Left.Process;
        }
        public static IManagedTask GetTaskObject(Guid taskId)
        {
            return getTaskImpl(taskId).Left.Task;
        }
        public static IManagedTask GetTaskObject(string taskName)
        {
            Instance.TaskLock.EnterReadLock();
            if (!Instance.TaskNames.ContainsKey(taskName))
            {
                Instance.TaskLock.ExitReadLock();
                return null;
            }
            Instance.TaskLock.ExitReadLock();
            return getTaskImpl(Instance.TaskNames[taskName]).Left.Task;
        }

        public static MessageChannel GetChannel(Guid taskId)
        {
            return getTaskImpl(taskId).Left.Channel;
        }
        public static MessageChannel GetChannel(string taskName)
        {
            Instance.TaskLock.EnterReadLock();
            if (!Instance.TaskNames.ContainsKey(taskName))
            {
                Instance.TaskLock.ExitReadLock();
                return null;
            }
            Instance.TaskLock.ExitReadLock();
            return getTaskImpl(Instance.TaskNames[taskName]).Left.Channel;
        }

        // Recurses through 
        private static TaskEntry getTaskImpl(Guid taskId)
        {
            Instance.TaskLock.EnterReadLock();
            if (!Instance.Tasks.ContainsKey(taskId))
            {
                Instance.TaskLock.ExitReadLock();
                return null;
            }
            var taskData = Instance.Tasks[taskId];
            if (taskData.GetState() == TaskEntry.State.Invalid)
            {
                Instance.TaskLock.ExitReadLock();
                return null;
            }
            if (taskData.GetState() == TaskEntry.State.Left)
            {
                Instance.TaskLock.ExitReadLock();
                return taskData;
            }
            Instance.TaskLock.ExitReadLock();
            return getTaskImpl(taskData);
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
            Instance.TaskLock.EnterUpgradeableReadLock();
            if (Instance.Tasks.ContainsKey(taskId))
            {
                Instance.TaskLock.ExitReadLock();
                throw new Exception("Task with id '" + taskId.ToString() + "' already registered.");
            }
            if (name != null && Instance.TaskNames.ContainsKey(name))
            {
                Instance.TaskLock.ExitReadLock();
                throw new Exception("Task with name '" + name + "' already registered.");
            }
            Instance.TaskLock.EnterWriteLock();
            Instance.Tasks[taskId] = new TaskEntry((taskInstance, task, channel));
            if (name != null)
                Instance.TaskNames[name] = taskId;
            Instance.TaskLock.ExitWriteLock();
            Instance.TaskLock.ExitUpgradeableReadLock();
            return taskId;
        }

        public static void RestartTask(Guid taskId, bool doRestoreState = false)
        {
            var (task, process) = GetTask(taskId);
            var messageChannel = GetChannel(taskId);
            Dictionary<string, dynamic> state = null;
            task.Terminate();
            process.Join();
            if (doRestoreState)
                state = StateBackup.DumpState(task);
            process = task.Run(taskId, messageChannel, context: new TaskContext(), state: state);
            Instance.TaskLock.EnterWriteLock();
            Instance.Tasks[taskId] = new TaskEntry((task, process, messageChannel));
            Instance.TaskLock.ExitWriteLock();
        }

        public static void RestartTask(string taskName, bool doRestoreState = false)
        {
            var (task, process) = GetTask(taskName);
            var messageChannel = GetChannel(taskName);
            Dictionary<string, dynamic> state = null;
            task.Terminate();
            process.Join();
            task.Terminate();
            if (doRestoreState)
                state = StateBackup.DumpState(task);
            process = task.Run(GetTaskObject(taskName).Id, messageChannel, context: new TaskContext(), state: state);
            Instance.TaskLock.EnterWriteLock();
            Instance.Tasks[task.Id] = new TaskEntry((task, process, messageChannel));
            Instance.TaskLock.ExitWriteLock();
        }

        public static void TerminateTask(Guid taskId, string reason = null, params dynamic[] args)
        {
            var (task, proc) = GetTask(taskId);
            task.Terminate(args: args);
            GetChannel(taskId).Close();
        }

        public static void TerminateTask(string taskName, params dynamic[] args)
        {
            var (task, proc) = GetTask(taskName);
            task.Terminate(args: args);
            GetChannel(taskName).Close();
        }
        public static void Send(Guid taskId, (string, AsyncCallHandle) message)
        {
            var taskObject = GetTask(taskId);
            if (taskObject.Item2 == null || taskObject.Item2 == null)
            {
                throw new Exception("No task found with id '" + taskId.ToString() + "'");
            }
            GetChannel(taskId).Send(message);
        }
        public static void Send(string taskName, (string, AsyncCallHandle) message)
        {
            var taskObject = GetTask(taskName);
            if (taskObject.Item2 == null || taskObject.Item2 == null)
            {
                throw new Exception("No task found with name '" + taskName + "'");
            }
            GetChannel(taskName).Send(message);
        }
        public static void CleanupTasks()
        {
            foreach(var taskEntry in Instance.Tasks)
            {
                var (task, proc) = taskEntry.Value.GetState() == TaskEntry.State.Left ?
                    GetTask(taskEntry.Key) :
                    GetTask(taskEntry.Value.Right);
                if (task.Alive)
                {
                    TerminateTask(task.Id);
                    proc.Join();
                }
            }
        }
    }
}

using Multiplayer.Actor.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using ActorEntry =
    Multiplayer.Collections.Either<
        (
            Multiplayer.Actor.IActor Task,
            System.Threading.Thread Process,
            Multiplayer.IPC.Channel<(string, Multiplayer.Actor.Runtime.ActorCallbackHandle)> Channel
        ),
        System.Guid>;
using MessageChannel = Multiplayer.IPC.Channel<(string, Multiplayer.Actor.Runtime.ActorCallbackHandle)>;

namespace Multiplayer.Actor
{
    internal class ActorRegistry
    {
        Dictionary<Guid, ActorEntry> Actors;
        Dictionary<string, Guid> ActorNames;
        ReaderWriterLockSlim ActorLock;
        private ActorRegistry()
        {
            Actors = new Dictionary<Guid, ActorEntry>();
            ActorNames = new Dictionary<string, Guid>();
            ActorLock = new ReaderWriterLockSlim();
        }

        public static ActorRegistry Instance { get { return Internal.instance; } }
        private class Internal
        {
            static Internal() { }
            internal static readonly ActorRegistry instance = new ActorRegistry();
        }

        public static (IActor, Thread) GetActorRuntime(Guid taskId)
        {
            var taskInfo = GetActorImpl(taskId);
            return (taskInfo.Left.Task, taskInfo.Left.Process);
        }
        public static (IActor, Thread) GetActorRuntime(string taskName)
        {
            Instance.ActorLock.EnterReadLock();
            if (!Instance.ActorNames.ContainsKey(taskName))
            {
                Instance.ActorLock.ExitReadLock();
                return (null, null);
            }
            Instance.ActorLock.ExitReadLock();
            var taskInfo = GetActorImpl(Instance.ActorNames[taskName]);
            return (taskInfo.Left.Task, taskInfo.Left.Process);
        }

        public static Thread GetActorProcess(Guid taskId)
        {
            return GetActorImpl(taskId).Left.Process;
        }
        public static Thread GetActorProcess(string taskName)
        {
            Instance.ActorLock.EnterReadLock();
            if (!Instance.ActorNames.ContainsKey(taskName))
            {
                Instance.ActorLock.ExitReadLock();
                return null;
            }
            Instance.ActorLock.ExitReadLock();
            return GetActorImpl(Instance.ActorNames[taskName]).Left.Process;
        }
        public static IActor GetActor(Guid taskId)
        {
            return GetActorImpl(taskId).Left.Task;
        }
        public static IActor GetActor(string taskName)
        {
            Instance.ActorLock.EnterReadLock();
            if (!Instance.ActorNames.ContainsKey(taskName))
            {
                Instance.ActorLock.ExitReadLock();
                return null;
            }
            Instance.ActorLock.ExitReadLock();
            return GetActorImpl(Instance.ActorNames[taskName]).Left.Task;
        }

        public static MessageChannel GetChannel(Guid taskId)
        {
            return GetActorImpl(taskId).Left.Channel;
        }
        public static MessageChannel GetChannel(string taskName)
        {
            Instance.ActorLock.EnterReadLock();
            if (!Instance.ActorNames.ContainsKey(taskName))
            {
                Instance.ActorLock.ExitReadLock();
                return null;
            }
            Instance.ActorLock.ExitReadLock();
            return GetActorImpl(Instance.ActorNames[taskName]).Left.Channel;
        }

        // Recurses through 
        private static ActorEntry GetActorImpl(Guid taskId)
        {
            Instance.ActorLock.EnterReadLock();
            if (!Instance.Actors.ContainsKey(taskId))
            {
                Instance.ActorLock.ExitReadLock();
                return null;
            }
            var taskData = Instance.Actors[taskId];
            if (taskData.GetState() == ActorEntry.State.Invalid)
            {
                Instance.ActorLock.ExitReadLock();
                return null;
            }
            if (taskData.GetState() == ActorEntry.State.Left)
            {
                Instance.ActorLock.ExitReadLock();
                return taskData;
            }
            Instance.ActorLock.ExitReadLock();
            return GetActorImpl(taskData);
        }

        public static Guid Start(IActor taskInstance, string name = null, ITaskContext context = null)
        {
            if (taskInstance.Status != ManagedTaskStatus.Created)
            {
                throw new Exception("Cannot start a task that is already running. Do not call OnStart or spawn tasks directly.");
            }
            var taskId = Guid.NewGuid();
            context = context ?? new TaskContext();
            var channel = new MessageChannel();
            var task = taskInstance.Run(taskId, channel, context);
            Instance.ActorLock.EnterUpgradeableReadLock();
            if (Instance.Actors.ContainsKey(taskId))
            {
                Instance.ActorLock.ExitReadLock();
                throw new Exception("Task with id '" + taskId.ToString() + "' already registered.");
            }
            if (name != null && Instance.ActorNames.ContainsKey(name))
            {
                Instance.ActorLock.ExitReadLock();
                throw new Exception("Task with name '" + name + "' already registered.");
            }
            Instance.ActorLock.EnterWriteLock();
            Instance.Actors[taskId] = new ActorEntry((taskInstance, task, channel));
            if (name != null)
                Instance.ActorNames[name] = taskId;
            Instance.ActorLock.ExitWriteLock();
            Instance.ActorLock.ExitUpgradeableReadLock();
            while(!taskInstance.Initialized) { }
            return taskId;
        }


        public static Guid StartAsync(IActor taskInstance, string name = null, ITaskContext context = null)
        {
            if (taskInstance.Status != ManagedTaskStatus.Created)
            {
                throw new Exception("Cannot start a task that is already running. Do not call OnStart or spawn tasks directly.");
            }
            var taskId = Guid.NewGuid();
            context = context ?? new TaskContext();
            var channel = new MessageChannel();
            var task = taskInstance.Run(taskId, channel, context);
            Instance.ActorLock.EnterUpgradeableReadLock();
            if (Instance.Actors.ContainsKey(taskId))
            {
                Instance.ActorLock.ExitReadLock();
                throw new Exception("Task with id '" + taskId.ToString() + "' already registered.");
            }
            if (name != null && Instance.ActorNames.ContainsKey(name))
            {
                Instance.ActorLock.ExitReadLock();
                throw new Exception("Task with name '" + name + "' already registered.");
            }
            Instance.ActorLock.EnterWriteLock();
            Instance.Actors[taskId] = new ActorEntry((taskInstance, task, channel));
            if (name != null)
                Instance.ActorNames[name] = taskId;
            Instance.ActorLock.ExitWriteLock();
            Instance.ActorLock.ExitUpgradeableReadLock();
            return taskId;
        }



        public static void Restart(Guid taskId, bool doRestoreState = false)
        {
            var (task, process) = GetActorRuntime(taskId);
            var messageChannel = GetChannel(taskId);
            Dictionary<string, dynamic> state = null;
            task.Terminate();
            process.Join();
            if (doRestoreState)
                state = StateBackup.DumpState(task);
            process = task.Run(taskId, messageChannel, context: new TaskContext(), state: state);
            Instance.ActorLock.EnterWriteLock();
            Instance.Actors[taskId] = new ActorEntry((task, process, messageChannel));
            Instance.ActorLock.ExitWriteLock();
        }

        public static void Restart(string taskName, bool doRestoreState = false)
        {
            var (task, process) = GetActorRuntime(taskName);
            var messageChannel = GetChannel(taskName);
            Dictionary<string, dynamic> state = null;
            task.Terminate();
            process.Join();
            task.Terminate();
            if (doRestoreState)
                state = StateBackup.DumpState(task);
            process = task.Run(GetActor(taskName).Id, messageChannel, context: new TaskContext(), state: state);
            Instance.ActorLock.EnterWriteLock();
            Instance.Actors[task.Id] = new ActorEntry((task, process, messageChannel));
            Instance.ActorLock.ExitWriteLock();
        }

        public static void Terminate(Guid taskId, string reason = null, params dynamic[] args)
        {
            var (task, proc) = GetActorRuntime(taskId);
            task.Terminate(args: args);
            GetChannel(taskId).Close();
        }

        public static void Terminate(string taskName, params dynamic[] args)
        {
            var (task, proc) = GetActorRuntime(taskName);
            task.Terminate(args: args);
            GetChannel(taskName).Close();
        }
        public static void Send(Guid taskId, (string, ActorCallbackHandle) message)
        {
            var taskObject = GetActorRuntime(taskId);
            if (taskObject.Item2 == null || taskObject.Item2 == null)
            {
                throw new Exception("No task found with id '" + taskId.ToString() + "'");
            }
            GetChannel(taskId).Send(message);
        }
        public static void Send(string taskName, (string, ActorCallbackHandle) message)
        {
            var taskObject = GetActorRuntime(taskName);
            if (taskObject.Item2 == null || taskObject.Item2 == null)
            {
                throw new Exception("No task found with name '" + taskName + "'");
            }
            GetChannel(taskName).Send(message);
        }
        public static void CleanupTasks()
        {
            foreach(var taskEntry in Instance.Actors)
            {
                var (task, proc) = taskEntry.Value.GetState() == ActorEntry.State.Left ?
                    GetActorRuntime(taskEntry.Key) :
                    GetActorRuntime(taskEntry.Value.Right);
                if (task.Alive)
                {
                    Terminate(task.Id);
                    proc.Join();
                }
            }
        }
    }
}

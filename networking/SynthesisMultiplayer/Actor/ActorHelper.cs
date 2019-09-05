using Multiplayer.Actor.Runtime;
using System;
using System.Threading.Tasks;

namespace Multiplayer.Actor
{
    public class ActorHelper
    {
        public static void Send(Guid taskId, (string, ActorCallbackHandle) message) => ActorRegistry.Send(taskId, message);
        public static void Send(string taskName, (string, ActorCallbackHandle) message) => ActorRegistry.Send(taskName, message);
        public static Guid Start(IActor task, string name = null) => ActorRegistry.Start(task, name, new TaskContext());
        public static void Restart(Guid taskId, bool doRestoreState = true) => ActorRegistry.Restart(taskId, doRestoreState);
        public static void Restart(string taskName, bool doRestoreState = true) => ActorRegistry.Restart(taskName, doRestoreState);
        public static IActor GetTask(Guid taskId) => ActorRegistry.GetActor(taskId);
        public static IActor GetTask(string taskName) => ActorRegistry.GetActor(taskName);
        public static dynamic Call(Guid taskId, string method, params dynamic[] args) => GetTask(taskId).Call(method, args: args);
        public static Task<dynamic> Call(string taskName, string method, params dynamic[] args) => GetTask(taskName).Call(method, args: args);
        public static Task Do(Guid taskId, string method, params dynamic[] args) => GetTask(taskId).Do(method, args: args);
        public static Task Do(string taskName, string method, params dynamic[] args) => GetTask(taskName).Do(method, args: args);
        public static void Terminate(Guid taskId, string reason = null, params dynamic[] args) => ActorRegistry.Terminate(taskId, reason, args);
        public static void Terminate(string taskName, string reason = null, params dynamic[] args) => ActorRegistry.Terminate(taskName, reason, args);
        public static void CleanupTasks() => ActorRegistry.CleanupTasks();
    }
}
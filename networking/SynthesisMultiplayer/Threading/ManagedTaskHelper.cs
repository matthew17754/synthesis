using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public class ManagedTaskHelper
    {
        public static void Send(Guid taskId, (string, AsyncCallHandle) message) => ManagedTaskRegistry.Send(taskId, message);
        public static void Send(string taskName, (string, AsyncCallHandle) message) => ManagedTaskRegistry.Send(taskName, message);
        public static Guid Start(IManagedTask task, string name = null) => ManagedTaskRegistry.StartTask(task, name, new TaskContext());
        public static void Restart(Guid taskId, bool doRestoreState = true) => ManagedTaskRegistry.RestartTask(taskId, doRestoreState);
        public static void Restart(string taskName, bool doRestoreState = true) => ManagedTaskRegistry.RestartTask(taskName, doRestoreState);
        public static IManagedTask GetTask(Guid taskId) => ManagedTaskRegistry.GetTaskObject(taskId);
        public static IManagedTask GetTask(string taskName) => ManagedTaskRegistry.GetTaskObject(taskName);
        public static Task<dynamic> Call(Guid taskId, string method, params dynamic[] args) => GetTask(taskId).Call(method, args: args);
        public static Task<dynamic> Call(string taskName, string method, params dynamic[] args) => GetTask(taskName).Call(method, args: args);
        public static Task Do(Guid taskId, string method, params dynamic[] args) => GetTask(taskId).Do(method, args: args);
        public static Task Do(string taskName, string method, params dynamic[] args) => GetTask(taskName).Do(method, args: args);
        public static void Terminate(Guid taskId, params dynamic[] args) => ManagedTaskRegistry.TerminateTask(taskId, args);
        public static void Terminate(string taskName, params dynamic[] args) => ManagedTaskRegistry.TerminateTask(taskName, args);
    }
}
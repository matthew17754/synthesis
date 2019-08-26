using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Util;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using TaskMethods = System.Collections.Generic.Dictionary<string, string>;
using Callbacks = System.Collections.Generic.Dictionary<string, SynthesisMultiplayer.Threading.Execution.ManagedTaskCallback>;
using StateData = System.Collections.Generic.Dictionary<string, dynamic>;

namespace SynthesisMultiplayer.Threading.Execution
{
    public delegate void ManagedTaskCallback(ITaskContext context, AsyncCallHandle handle = null);
    public interface IManagedTask : IDisposable
    {

        bool Alive { get; }
        bool Initialized { get; }
        Guid Id { get; }
        ManagedTaskStatus Status { get; }

        void Initialize(Guid id);
        void Terminate(string reason = null, params dynamic[] args);

        void Loop();
    }

    public enum ManagedTaskStatus
    {
        Created,
        Initialized,
        Ready,
        Running,
        Fault,
        Canceled,
        Completed
    }

    public static class IManagedTaskMethods
    {
        public static Task Run(this IManagedTask task, Guid taskId, Channel<(string, AsyncCallHandle)> channel, ITaskContext context = null, int loopTime = 50, StateData state = null)
        {
            Callbacks callbacks = GenerateCallbackList(task);
            TaskMethods taskMethods = GenerateMethodList(task);
            return Task.Factory.StartNew((c) =>
            {
                task.Initialize(taskId);
                if (state != null)
                    StateBackup.RestoreState(task, state);
                while (task.Alive)
                {
                    if (task.Initialized)
                    {
                        var message = channel.TryGet();
                        if (message.Valid)
                        {
                            var (callback, handle) = message.Get();
                            if (!taskMethods.ContainsKey(callback))
                                throw new Exception("Unknown callback: '" + callback + "'");
                            callbacks[taskMethods[callback]](context ?? new TaskContext(), handle);
                        }
                        task.Loop();
                    }
                    Thread.Sleep(loopTime);
                }
            }, context, TaskCreationOptions.LongRunning);
        }

        public static Task<dynamic> Call(this IManagedTask task, string method, params dynamic[] args)
        {
            return Task<dynamic>.Factory.StartNew(() =>
            {
                var handle = new AsyncCallHandle(args);
                ManagedTaskHelper.Send(task.Id, (method, handle));
                return handle.Result;
            });
        }
        public static Task Do(this IManagedTask task, string method, int methodCallWaitPeriod = 50, params dynamic[] args)
        {
            return Task.Factory.StartNew(() =>
            {
                var handle = new AsyncCallHandle(args);
                ManagedTaskHelper.Send(task.Id, (method, handle));
                handle.Wait();
                return;
            });
        }

        public static Callbacks GenerateCallbackList(this IManagedTask task) => task
            .GetType().GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(Callback), false).Length > 0).ToList()
            .Select(method =>
            {
                var callbackInfo = (Callback)method
                    .GetCustomAttribute(typeof(Callback), false);
                var callbackName = method.DeclaringType.Name +
                    (callbackInfo.Name ?? method.Name);
                return (callbackName, (ManagedTaskCallback)Delegate
                   .CreateDelegate(typeof(ManagedTaskCallback), task, method));
            }).ToDictionary(x => x.callbackName, x => x.Item2);

        public static TaskMethods GenerateMethodList(this IManagedTask task) => task
            .GetType().GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(Callback), false).Length > 0).ToList()
            .Select(method =>
            {
                var callbackInfo = (Callback)method
                    .GetCustomAttribute(typeof(Callback), false);
                var callbackName = method.DeclaringType.Name +
                    (callbackInfo.Name ?? method.Name);
                return (callbackInfo.MethodName, callbackName);
            }).Where(kv => kv.MethodName != null).ToDictionary(x => x.MethodName, x => x.callbackName);
    }
}

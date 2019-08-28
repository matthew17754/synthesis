using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Util;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SynthesisMultiplayer.Threading.Runtime;
using static SynthesisMultiplayer.Threading.Runtime.ArgumentPacker;
using TaskMethods = System.Collections.Generic.Dictionary<string, string>;
using Methods = System.Collections.Generic.Dictionary<string, 
    (
        SynthesisMultiplayer.Threading.ManagedTaskMethod Method,
        SynthesisMultiplayer.Threading.Runtime.CallbackInfo MethodInfo
    )>;
using StateData = System.Collections.Generic.Dictionary<string, dynamic>;
using System.Collections.Generic;
using System.Diagnostics;

namespace SynthesisMultiplayer.Threading
{
    public delegate void ManagedTaskMethod(ITaskContext context, AsyncCallHandle handle = null);
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
            Methods Methods = GenerateCallbackList(task);
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
                            var (Method, handle) = message.Get();
                            if (!taskMethods.ContainsKey(Method))
                                throw new Exception("Unknown Method: '" + Method + "'");
                            Methods[taskMethods[Method]].Method(context ?? new TaskContext(), handle);
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
                var handle = new AsyncCallHandle(GetMethodInfo(task.GetType(), method), args);
                ManagedTaskHelper.Send(task.Id, (method, handle));
                return handle.Result;
            });
        }
        public static Task Do(this IManagedTask task, string method, int methodCallWaitPeriod = 50, params dynamic[] args)
        {
            return Task.Factory.StartNew(() =>
            {
                var handle = new AsyncCallHandle(GetMethodInfo(task.GetType(), method), args);
                ManagedTaskHelper.Send(task.Id, (method, handle));
                handle.Wait();
                return;
            });
        }

        public static Methods GenerateCallbackList(this IManagedTask task) => task
            .GetType().GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(CallbackAttribute), false).Length > 0).ToList()
            .Select(method =>
            {
                var Method = (CallbackAttribute)method
                    .GetCustomAttribute(typeof(CallbackAttribute), false);
                var MethodName = method.DeclaringType.Name + method.Name;
                var Arguments = (Method.ArgumentNames ?? new List<string>())
                    .Join(method.GetCustomAttributes(typeof(ArgumentAttribute), false)
                    .Cast<ArgumentAttribute>()
                    .ToList(),
                    name => name,
                    arg => arg.Name,
                    (argName, arg) => arg).ToList();
                var ReturnType = (ReturnTypeAttribute)method
                    .GetCustomAttribute(typeof(ReturnTypeAttribute), false);
                var MethodInfo = new Runtime.CallbackInfo(MethodName, Arguments, ReturnType);
                return (MethodName, ((ManagedTaskMethod)Delegate
                   .CreateDelegate(typeof(ManagedTaskMethod), task, method),MethodInfo));
            }).ToDictionary(x => x.MethodName, x => x.Item2);
        
        public static TaskMethods GenerateMethodList(this IManagedTask task) => task
            .GetType().GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(CallbackAttribute), false).Length > 0).ToList()
            .Select(method =>
            {
                var Method = (CallbackAttribute)method
                    .GetCustomAttribute(typeof(CallbackAttribute), false);
                var CallbackName = method.DeclaringType.Name + method.Name;
                return (Method.Name, CallbackName);
            }).Where(kv => kv.Name != null).ToDictionary(x => x.Name, x => x.CallbackName);
    }
}

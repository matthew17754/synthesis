using Multiplayer.Attribute;
using Multiplayer.Util;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Multiplayer.Actor.Runtime;
using static Multiplayer.Actor.Runtime.ArgumentUnpacker;
using Callbacks = System.Collections.Generic.Dictionary<string, string>;
using MessageHandles = System.Collections.Generic.Dictionary<string, 
    (
        Multiplayer.Actor.ActorCallback Method,
        Multiplayer.Actor.Runtime.ActorCallbackInfo MethodInfo
    )>;
using StateData = System.Collections.Generic.Dictionary<string, dynamic>;
using System.Collections.Generic;

namespace Multiplayer.Actor
{
    public delegate void ActorCallback(ITaskContext context, ActorCallbackHandle handle = null);
    public interface IActor : IDisposable
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

    public static class IActorMethods
    {
        public static Thread Run(this IActor task, Guid taskId, Channel<(string, ActorCallbackHandle)> channel, ITaskContext context = null, int loopTime = 50, StateData state = null)
        {
            MessageHandles Methods = GetMethodHandles(task);
            Callbacks taskMethods = GetCallbacks(task);
            var t = new Thread((c) =>
            {
                task.Initialize(taskId);
                if (state != null)
                    StateBackup.RestoreState(task, state);
                while (task.Alive)
                {
                    //Info.Log($"Loop for: {task.GetType().Name}");
                    if (task.Initialized)
                    {
                        if (channel.TryPeek().Valid)
                        {
                            var (method, handle) = channel.Get();
                            if (!taskMethods.ContainsKey(method))
                                throw new Exception("Unknown Method: '" + method + "'");
                            Methods[taskMethods[method]].Method(context ?? new TaskContext(), handle);
                        }
                        task.Loop();
                    }
                    Thread.Sleep(loopTime);
                }
            });
            t.Start(context);
            return t;
        }

        public static Task<dynamic> Call(this IActor task, string method, params dynamic[] args)
        {
            return Task.Run(() =>
            {
                var Method = method;
                var handle = new ActorCallbackHandle(GetMethodInfo(task.GetType(), method), args);
                ActorHelper.Send(task.Id, (method, handle));
                return handle.Result;
            });
        }
        public static Task Do(this IActor task, string method, int methodCallWaitPeriod = 50, params dynamic[] args)
        {
            return Task.Run(() =>
            {
                var Method = method;
                var handle = new ActorCallbackHandle(GetMethodInfo(task.GetType(), method), args);
                ActorHelper.Send(task.Id, (method, handle));
                handle.Wait();
                return;
            });
        }

        public static MessageHandles GetMethodHandles(this IActor task) => task
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
                var MethodInfo = new Runtime.ActorCallbackInfo(MethodName, Arguments, ReturnType);
                return (MethodName, ((ActorCallback)Delegate
                   .CreateDelegate(typeof(ActorCallback), task, method),MethodInfo));
            }).ToDictionary(x => x.MethodName, x => x.Item2);
        
        public static Callbacks GetCallbacks(this IActor task) => task
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

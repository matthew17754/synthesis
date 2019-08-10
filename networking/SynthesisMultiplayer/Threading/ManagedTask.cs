using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Threading.Methods;
using System.Linq.Expressions;
using System.Reflection;

using StateData = System.Collections.Generic.Dictionary<string, dynamic>;
namespace SynthesisMultiplayer.Threading
{
    public abstract class ManagedTask : IManagedTask
    {

        private bool disposed = false;
        private Dictionary<string, ManagedTaskCallback> CallbackRegistry;
        private Dictionary<string, string> MethodRegistry;
        protected bool Alive { get; set; }
        protected bool Paused { get; set; }
        protected int MethodCallWaitPeriod = 10; // ms
        protected string State { get; set; }
        protected ManagedTaskStatus Status { get; set; }
        protected Channel<(string, AsyncCallHandle)> MessageChannel;
        public bool IsAlive() => Alive;
        public bool IsPaused() => Paused;

        protected void checkReady()
        {
            if (!IsReady())
                throw new Exception("Attempt to interact with Managed task that is not ready.");
            return;
        }

        public ManagedTask()
        {
            CallbackRegistry = new Dictionary<string, ManagedTaskCallback>();
            MethodRegistry = new Dictionary<string, string>();
            Status = ManagedTaskStatus.Created;
        }

        public virtual void Initialize()
        {
            MessageChannel = new Channel<(string, AsyncCallHandle)>();
            GetType().GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(Callback), false).Length > 0).ToList()
                .ForEach(method =>
                {
                    var callbackInfo = (Callback)method
                        .GetCustomAttribute(typeof(Callback), false);
                    var callbackName = method.DeclaringType.Name +
                        (callbackInfo.Name ?? method.Name);
                    RegisterCallback(callbackName, (ManagedTaskCallback)Delegate
                        .CreateDelegate(typeof(ManagedTaskCallback), this, method));
                    if (callbackInfo.MethodName != null)
                        RegisterMethod(callbackInfo.MethodName, callbackName);
                });

            Status = ManagedTaskStatus.Initialized;
        }

        public virtual bool IsReady() => Status != ManagedTaskStatus.Created 
            && Status != ManagedTaskStatus.Canceled
            && Status != ManagedTaskStatus.Fault;

        protected void RegisterCallback(string name, ManagedTaskCallback callback)
        {
            if (CallbackRegistry.ContainsKey(name))
                throw new Exception("Callback '" + name + "' already registered");
            CallbackRegistry[name] = callback;
        }
        protected void RegisterMethod(string name, string methodName)
        {
            if (MethodRegistry.ContainsKey(name))
            {
                Console.WriteLine("Warning: overwriting method '"
                    + name + "' from old '"
                    + MethodRegistry[name]
                    + "' to '" + methodName + "'");
            }
            MethodRegistry[name] = methodName;
        }

        public void SendMessage(string message, AsyncCallHandle handle) => MessageChannel.Send((message, handle));
        public string GetState()
        {
            checkReady();
            return State;
        }
        public Task<dynamic> Call(string method, params dynamic[] args)
        {
            checkReady();
            return Task<dynamic>.Factory.StartNew(() =>
            {
                var methodHandle = new AsyncCallHandle(args);
                if (!MethodRegistry.ContainsKey(method))
                    return null;
                MessageChannel.Send((MethodRegistry[method], methodHandle));
                while (!methodHandle.Ready)
                {
                    if (methodHandle.Fault)
                    {
                        return null;
                    }
                    Thread.Sleep(MethodCallWaitPeriod);
                }
                return methodHandle.Result;
            });
        }
        public Task Do(string method, params dynamic[] args)
        {
            checkReady();
            return Task.Factory.StartNew(() =>
            {
                var methodHandle = new AsyncCallHandle(args);
                if (!MethodRegistry.ContainsKey(method))
                    return;
                MessageChannel.Send((MethodRegistry[method], methodHandle));
                while (!methodHandle.Ready)
                {
                    if (methodHandle.Fault)
                    {
                        return;
                    }
                    Thread.Sleep(MethodCallWaitPeriod);
                }
                return;
            });

        }

        public virtual StateData DumpState(StateData currentState) =>
            GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Cast<MemberInfo>()
                .Concat(GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(p => p.CanWrite && p.CanRead))
                .Where(f => f.GetCustomAttributes(typeof(SavedState), false).Length > 0).Select(field =>
                {
                    var stateInfo = (SavedState)field.GetCustomAttribute(typeof(SavedState));
                    var stateName = stateInfo.Name ?? field.DeclaringType.ToString() + "." + field.Name;
                    if (currentState.ContainsKey(stateName))
                        throw new Exception("Duplicate state entry '" + stateName + "'");
                    return (stateName, field.MemberType == MemberTypes.Field ?
                        (dynamic)((FieldInfo)field).GetValue(this) : (dynamic)((PropertyInfo)field).GetValue(this));
                }).Concat(currentState.ToList().Select(kv => (kv.Key, kv.Value))).ToDictionary(x => x.Item1, x => x.Item2);

        public virtual void RestoreState(StateData state) => state.ToList().ForEach(kv =>
                GetType().GetField(kv.Key.Substring(kv.Key.IndexOf('.')), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(this, kv.Value)
            );

        public void Cancel() => Status = ManagedTaskStatus.Canceled;

        // Public APIs
        [Callback(methodName: Default.Task.Start)]
        public void Start(ITaskContext context, AsyncCallHandle handle = null)
        {
            checkReady();
            SendMessage(Default.Internal.OnStart, null);
            handle.Done();
        }
        [Callback(methodName: Default.Task.Resume)]
        public void Resume(ITaskContext context, AsyncCallHandle handle = null)
        {
            checkReady();
            SendMessage(Default.Internal.OnResume, null);
            handle.Done();
        }

        [Callback(methodName: Default.Task.Pause)]
        public void Pause(ITaskContext context, AsyncCallHandle handle = null)
        {
            checkReady();
            SendMessage(Default.Internal.OnPause, null);
            handle.Done();
        }
        [Callback(methodName: Default.Task.Stop)]
        public void Stop(ITaskContext context, AsyncCallHandle handle = null)
        {
            checkReady();
            SendMessage(Default.Internal.OnStop, null);
            handle.Done();
        }
        [Callback(methodName: Default.Task.Exit)]
        public void Exit(ITaskContext context, AsyncCallHandle handle = null)
        {
            checkReady();
            SendMessage(Default.Internal.OnExit, null);
            handle.Done();
        }
        public ManagedTaskStatus GetStatus() => Status;
        // Internal Callbacks
        public virtual void OnStart(ITaskContext context, AsyncCallHandle handle)
        {
            Alive = true;
            Status = ManagedTaskStatus.Running;
        }
        public virtual void OnResume(ITaskContext context, AsyncCallHandle handle)
        {
            Status = ManagedTaskStatus.Running;
        }
        public virtual void OnCycle(ITaskContext context, AsyncCallHandle handle) { }
        public virtual void OnPause(ITaskContext context, AsyncCallHandle handle) { }
        public virtual void OnStop(ITaskContext context, AsyncCallHandle handle) { }
        public virtual void OnExit(ITaskContext context, AsyncCallHandle handle)
        {
            Alive = false;
            Status = ManagedTaskStatus.Completed;
        }

        public void OnMessage(ITaskContext context, AsyncCallHandle handle)
        {
            checkReady();
            var messageData = MessageChannel.TryGet();
            if (messageData.IsValid())
            {

                var (callback, callHandle) = messageData.Get();
                switch (callback)
                {
                    case Default.Internal.OnStart:
                        OnStart(context, callHandle);
                        break;
                    case Default.Internal.OnResume:
                        OnResume(context, callHandle);
                        break;
                    case Default.Internal.OnPause:
                        OnPause(context, callHandle);
                        break;
                    case Default.Internal.OnStop:
                        OnStop(context, callHandle);
                        break;
                    case Default.Internal.OnExit:
                        OnExit(context, callHandle);
                        break;
                    default:
                        if (!CallbackRegistry.ContainsKey(callback))
                        {
                            throw new Exception("Unknown thread command");
                        }
                        CallbackRegistry[callback](context, callHandle);
                        break;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    MessageChannel.Dispose();
                    Status = ManagedTaskStatus.Canceled;
                }
                disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
    }
}

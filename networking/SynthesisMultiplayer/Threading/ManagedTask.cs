using SynthesisMultiplayer.Util;
using SynthesisMultiplayer.Threading.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SynthesisMultiplayer.Attribute;

namespace SynthesisMultiplayer.Threading
{
    public abstract class ManagedTask : IManagedTask
    {

        private bool disposed = false;
        private Dictionary<string, ManagedTaskCallback> callbackRegistery;
        private Dictionary<string, string> methodRegistery;
        private Dictionary<string, ManagedTaskCallback> overwrittenCallbacks;
        protected bool Alive { get; set; }
        protected bool Paused { get; set; }
        protected int MethodCallWaitPeriod = 10; // ms
        protected Channel<(string, AsyncCallHandle?)> StatusChannel, MessageChannel;
        protected string State { get; set; }
        public bool IsAlive() => Alive;
        public bool IsPaused() => Paused;

        public ManagedTask()
        {
            callbackRegistery = new Dictionary<string, ManagedTaskCallback>();
            methodRegistery = new Dictionary<string, string>();
            GetType().GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(Callback), false).Length > 0).ToList()
                .ForEach(method =>
                {
                    var callbackInfo = ((Callback)method
                        .GetCustomAttributes(typeof(Attribute.Callback), false)
                        .GetValue(0));
                    var callbackName = method.DeclaringType.Name + 
                        (callbackInfo.Name != null ? callbackInfo.Name : method.Name);
                    RegisterCallback(callbackName, (ManagedTaskCallback)Delegate
                        .CreateDelegate(typeof(ManagedTaskCallback), this, method));
                    if (callbackInfo.MethodName != null)
                        RegisterMethod(callbackInfo.MethodName, callbackName);
                });
        }

        protected void RegisterCallback(string name, ManagedTaskCallback callback)
        {
            if (callbackRegistery.ContainsKey(name))
                throw new Exception("Callback '" + name + "' already registered");
            callbackRegistery[name] = callback;
        }
        protected void RegisterMethod(string name, string methodMessage)
        {
            methodRegistery[name] = methodMessage;
        }

        public void SendMessage(string message, AsyncCallHandle? handle) => MessageChannel.Send((message, handle));
        public string GetState()
        {
            if (StatusChannel.TryPeek().IsValid())
                State = StatusChannel.Get().Item1;
            return State;
        }
        public Task<dynamic> Call(string method, params dynamic[] args)
        {
            return Task<dynamic>.Factory.StartNew(() =>
            {
                var methodHandle = new AsyncCallHandle(args);
                if (!methodRegistery.ContainsKey(method))
                    return null;
                MessageChannel.Send((methodRegistery[method], methodHandle));
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
            return Task.Factory.StartNew(() =>
            {
                var methodHandle = new AsyncCallHandle(args);
                if (!methodRegistery.ContainsKey(method))
                    return;
                MessageChannel.Send((methodRegistery[method], methodHandle));
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





        public virtual void OnStart(ITaskContext context, AsyncCallHandle? handle) => Alive = true;
        public virtual void OnResume(ITaskContext context, AsyncCallHandle? handle) { }
        public virtual void OnCycle(ITaskContext context, AsyncCallHandle? handle) { }
        public virtual void OnPause(ITaskContext context, AsyncCallHandle? handle) { }
        public virtual void OnStop(ITaskContext context, AsyncCallHandle? handle) { }
        public virtual void OnExit(ITaskContext context, AsyncCallHandle? handle) => StatusChannel.Send((Default.State.GracefulExit, null));
        public virtual void OnMessage(ITaskContext context, AsyncCallHandle? handle)
        {
            var messageData = MessageChannel.TryGet();
            if (messageData.IsValid())
            {

                var (message, callHandle) = messageData.Get();
                switch ((string)message)
                {
                    case Default.Task.Start:
                        OnStart(context, handle);
                        break;
                    case Default.Task.Resume:
                        OnResume(context, handle);
                        break;
                    case Default.Task.Pause:
                        OnPause(context, handle);
                        break;
                    case Default.Task.Stop:
                        OnStop(context, handle);
                        break;
                    case Default.Task.Exit:
                        OnExit(context, handle);
                        break;
                    default:
                        var messageName = (string)message;
                        if (!callbackRegistery.ContainsKey(messageName))
                        {
                            throw new Exception("Unknown thread command");
                        }
                        callbackRegistery[messageName](context, handle);
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

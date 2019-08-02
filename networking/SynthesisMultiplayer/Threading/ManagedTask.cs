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

        bool disposed = false;
        private IMessage state;
        protected bool Alive { get; set; }
        protected bool Paused { get; set; }
        protected int MethodCallWaitPeriod = 10; // ms
        protected Channel<(IMessage, AsyncCallHandle?)> StatusChannel, MessageChannel;
        protected Dictionary<string, ManagedTaskCallback> callbackRegistery;
        protected Dictionary<string, IMessage> methodRegistery;
        protected IMessage State { get; set; }
        public bool IsAlive() => Alive;
        public bool IsPaused() => Paused;

        public ManagedTask()
        {
            callbackRegistery = new Dictionary<string, ManagedTaskCallback>();
            methodRegistery = new Dictionary<string, IMessage>();
            foreach(var method in GetType().GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(Callback), false).Length > 0).ToArray())
            {
                var callbackInfo = ((Callback)method
                    .GetCustomAttributes(typeof(Attribute.Callback), false)
                    .GetValue(0));
                if (!typeof(IMessage).IsAssignableFrom(callbackInfo.CallbackMessageType))
                    throw new Exception("Attempt to register method with no message interface specified");
                Console.WriteLine("Callback: '" + callbackInfo.Name + "' was found");
                RegisterCallback(callbackInfo.Name, (ManagedTaskCallback)Delegate
                    .CreateDelegate(typeof(ManagedTaskCallback), this, method));
                RegisterMethod(callbackInfo.Name, (IMessage)Activator.CreateInstance(callbackInfo.CallbackMessageType));
            }
        }

        protected void RegisterCallback(string name, ManagedTaskCallback callback)
        {
            if (callbackRegistery.ContainsKey(name) || GetType().GetMethod(name) != null)
                throw new Exception("Callback '" + name + "' already registered or is a method");
            callbackRegistery[name] = callback;
        }
        protected void RegisterMethod(string name, IMessage methodMessage)
        {
            var duplicates = methodRegistery
                .Where(kv => kv.Value.GetType().IsEquivalentTo(methodMessage.GetType()))
                .ToArray();
                foreach(var dup in duplicates)
                {
                    if (dup.Key != name)
                    {
                    throw new Exception("Attempting to register multiple callbacks of type '" +
                        methodMessage.GetType() + "' under different method names");
                    }
                }
            if (methodRegistery.ContainsKey(name))
                throw new Exception("Method '" + name + "' already registered or is a method");
            methodRegistery[name] = methodMessage;
        }

        public void SendMessage(IMessage message, AsyncCallHandle? handle) => MessageChannel.Send((message, handle));
        public IMessage GetState()
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
        public virtual void OnExit(ITaskContext context, AsyncCallHandle? handle) => StatusChannel.Send((new Default.State.GracefulExitMessage(), null));
        public virtual void OnMessage(ITaskContext context, AsyncCallHandle? handle)
        {
            var messageData = MessageChannel.TryGet();
            if (messageData.IsValid())
            {

                var (message, callHandle) = messageData.Get();
                switch (((IMessage)message).GetName())
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
                        var messageName = ((IMessage)message).GetName();
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

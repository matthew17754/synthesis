using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Threading.Message;
using SynthesisMultiplayer.Util;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SynthesisMultiplayer.Common
{
    public class ManagedUDPTask : IManagedTask
    {
        protected bool Alive { get; set; }
        protected bool Paused { get; set; }
        protected bool disposedValue = false;
        protected IPEndPoint Endpoint;
        protected UdpClient Connection;
        protected IMessage LastState;
        Channel<IMessage> StatusChannel, MessageChannel;
        Dictionary<string, ManagedTaskCallback> callbackRegistery;
        public ManagedUDPTask(Channel<IMessage> statusChannel,
            Channel<IMessage> messageChannel,
            IPAddress ip,
            int port = 33000)
        {
            StatusChannel = statusChannel;
            MessageChannel = messageChannel;
            Endpoint = new IPEndPoint(ip, port);
            callbackRegistery = new Dictionary<string, ManagedTaskCallback>();
        }
        public void RegisterCallback(string name, ManagedTaskCallback callback)
        {
            if (callbackRegistery.ContainsKey(name) || GetType().GetMethod(name) != null)
                throw new Exception("Callback '" + name + "' already registered or is a method");
            callbackRegistery[name] = callback;
        }
        public bool IsAlive() => Alive;
        public bool IsPaused() => Paused;
        public void SendMessage(IMessage message) => MessageChannel.Send(message);
        public IMessage GetMessage() => StatusChannel.TryPeek().IsValid() ? StatusChannel.Get() : LastState;
        public virtual void OnStart(ref ITaskContext context) => Alive = true;
        public virtual void OnResume(ref ITaskContext context) { }
        public virtual void OnCycle(ref ITaskContext context) { }
        public virtual void OnPause(ref ITaskContext context) { }
        public virtual void OnStop(ref ITaskContext context) { }
        public virtual void OnExit(ref ITaskContext context) { }
        public virtual void OnMessage(ref ITaskContext context)
        {
            var message = MessageChannel.TryGet();
            if (message.IsValid())
            {
                switch (((IMessage)message).GetName())
                {
                    case Default.Task.Start:
                        OnStart(ref context);
                        break;
                    case Default.Task.Resume:
                        OnResume(ref context);
                        break;
                    case Default.Task.Pause:
                        OnPause(ref context);
                        break;
                    case Default.Task.Stop:
                        OnStop(ref context);
                        break;
                    case Default.Task.Exit:
                        OnExit(ref context);
                        break;
                    default:
                        var messageName = ((IMessage)message).GetName();
                        if (!callbackRegistery.ContainsKey(messageName))
                        {
                            throw new Exception("Unknown thread command");
                        }
                        callbackRegistery[messageName](ref context);
                        break;
                }
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Connection.Close();
                    Connection.Dispose();
                    MessageChannel.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

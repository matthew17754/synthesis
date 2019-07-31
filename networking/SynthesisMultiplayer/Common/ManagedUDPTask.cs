using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Threading.Message;
using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SynthesisMultiplayer.Common
{
    class ManagedUDPTask : IManagedTask
    {
        public enum ServerType
        {
            Broadcast,
            Listener,
            Sender,
            Receiver
        }

        protected int port { get; set; }
        protected bool alive;
        protected bool paused;
        bool disposedValue = false;
        IPAddress Ip;
        readonly ServerType Type;
        List<UdpClient> Connections;
        Channel<IMessage> StatusChannel, MessageChannel;
        Dictionary<string, ManagedTaskCallback> callbackRegistery;

        public ManagedUDPTask(Channel<IMessage> statusChannel,
            Channel<IMessage> messageChannel,
            ServerType type,
            IPAddress ip,
            int port = 33000)
        {
            StatusChannel = statusChannel;
            MessageChannel = messageChannel;
            Ip = ip;
            this.port = port;
            Type = type;
            Connections = new List<UdpClient>();
            callbackRegistery = new Dictionary<string, ManagedTaskCallback>();
        }

        public void RegisterCallback(string name, ManagedTaskCallback callback)
        {
            if (callbackRegistery.ContainsKey(name))
                throw new Exception("Callback '" + name + "' already registered");
            callbackRegistery[name] = callback;
        }

        public void SendMessage(IMessage message)
        {
            MessageChannel.Send(message);
        }

        public bool IsAlive() => alive;
        public bool IsPaused() => paused;

        public virtual void OnStart(ITaskContext context) { }

        public virtual void OnResume(ITaskContext context) { }

        public virtual void OnMessage(ITaskContext context)
        {
            var message = MessageChannel.TryGet();
            if (message.IsValid())
            {
                switch (((IMessage)message).GetName())
                {
                    case Default.Task.Start:
                        OnStart(context);
                        break;
                    case Default.Task.Resume:
                        OnResume(context);
                        break;
                    case Default.Task.Pause:
                        OnPause(context);
                        break;
                    case Default.Task.Stop:
                        OnStop(context);
                        break;
                    case Default.Task.Exit:
                        OnExit(context);
                        break;
                    default:
                        var messageName = ((IMessage)message).GetName();
                        if (!callbackRegistery.ContainsKey(messageName))
                        {
                            throw new Exception("Unknown thread command");
                        }
                        callbackRegistery[messageName](context);
                        break;
                }
            }
        }

        public virtual void OnCycle(ITaskContext context) { }

        public virtual void OnPause(ITaskContext context)
        {
            foreach (var conn in Connections)
            {
                switch (Type)
                {
                    case ServerType.Broadcast:
                    case ServerType.Sender:

                        break;
                    case ServerType.Listener:
                    case ServerType.Receiver:

                        break;
                }
            }
        }

        public virtual void OnStop(ITaskContext context) { }

        public virtual void OnExit(ITaskContext context) => Dispose();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var conn in Connections)
                    {
                        conn.Close();
                        Connections.Remove(conn);
                        conn.Dispose();
                    }
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

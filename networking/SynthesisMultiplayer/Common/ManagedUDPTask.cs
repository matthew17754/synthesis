using Multiplayer.Actor;
using Multiplayer.Actor.Runtime;
using Multiplayer.Attribute;
using Multiplayer.IPC;
using Multiplayer.Server;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Multiplayer.Common
{
    public abstract class ManagedUdpTask : IServer
    {
        private bool disposed;
        protected Mutex statusMutex;
        [SavedState]
        protected IPEndPoint Endpoint { get; set; }
        protected UdpClient Connection { get; set; }
        protected Channel<(string, ActorCallbackHandle)> Messages;
        public abstract bool Alive { get; }
        public abstract bool Initialized { get; }
        public ManagedTaskStatus Status { get; protected set; }
        public Guid Id { get; protected set; }

        public abstract void ServeCallback(ITaskContext context, ActorCallbackHandle handle);
        public abstract void RestartCallback(ITaskContext context, ActorCallbackHandle handle);
        public abstract void ShutdownCallback(ITaskContext context, ActorCallbackHandle handle);
        public abstract void Initialize(Guid taskId);
        public abstract void Terminate(string reason = null, params dynamic[] args);
        public abstract void Loop();

        public ManagedUdpTask(IPAddress ip, int port = 33000)
        {
            statusMutex = new Mutex();
            Endpoint = new IPEndPoint(ip, port);
            Messages = new Channel<(string, ActorCallbackHandle)>();

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Connection.Close();
                    Connection.Dispose();
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

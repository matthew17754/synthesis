using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Threading.Runtime;
using SynthesisMultiplayer.Util;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SynthesisMultiplayer.Common
{
    public abstract class ManagedUdpTask : IServer
    {
        private bool disposed;
        protected Mutex statusMutex;
        [SavedStateAttribute]
        protected IPEndPoint Endpoint { get; set; }
        protected UdpClient Connection { get; set; }
        protected Channel<(string, AsyncCallHandle)> Messages;
        public abstract bool Alive { get; }
        public abstract bool Initialized { get; }
        public ManagedTaskStatus Status { get; protected set; }
        public Guid Id { get; protected set; }

        public abstract void ServeMethod(ITaskContext context, AsyncCallHandle handle);
        public abstract void RestartMethod(ITaskContext context, AsyncCallHandle handle);
        public abstract void ShutdownMethod(ITaskContext context, AsyncCallHandle handle);
        public abstract void Initialize(Guid taskId);
        public abstract void Terminate(string reason = null, params dynamic[] args);
        public abstract void Loop();

        public ManagedUdpTask(IPAddress ip, int port = 33000)
        {
            statusMutex = new Mutex();
            Endpoint = new IPEndPoint(ip, port);
            Messages = new Channel<(string, AsyncCallHandle)>();

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

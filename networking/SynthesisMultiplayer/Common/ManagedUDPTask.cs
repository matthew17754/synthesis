using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Server;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Util;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SynthesisMultiplayer.Common
{
    public abstract class ManagedUDPTask : IManagedTask, IServer
    {
        private bool disposed;
        protected Mutex statusMutex;
        [SavedState]
        protected IPEndPoint Endpoint { get; set; }
        protected UdpClient Connection { get; set; }
        protected Channel<(string, AsyncCallHandle)> Messages;
        public abstract bool Alive { get; }
        public abstract bool Initialized { get; }
        public ManagedTaskStatus Status { get; protected set; }

        public void SendMessage((string, AsyncCallHandle) message)
        {
            Messages.Send(message);
        }
        public Optional<(string, AsyncCallHandle)> GetMessage()
        {
            return Messages.TryGet();
        }

        public abstract void Serve(ITaskContext context, AsyncCallHandle handle);
        public abstract void Restart(ITaskContext context, AsyncCallHandle handle);
        public abstract void Shutdown(ITaskContext context, AsyncCallHandle handle);
        public abstract void Initialize();
        public abstract void Terminate(string reason = null, System.Collections.Generic.Dictionary<string, dynamic> state = null);
        public abstract void Loop();

        public ManagedUDPTask(IPAddress ip, int port = 33000)
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

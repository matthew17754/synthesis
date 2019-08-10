using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Server;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Util;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SynthesisMultiplayer.Common
{
    public abstract class ManagedUDPTask : ManagedTask, IServer
    {
        private bool disposed;
        protected Mutex statusMutex;
        [SavedState]
        protected IPEndPoint Endpoint { get; set; }
        protected UdpClient Connection { get; set; }
        public ManagedUDPTask(IPAddress ip,
            int port = 33000) : base()
        {
            statusMutex = new Mutex();
            Endpoint = new IPEndPoint(ip, port);
        }
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Connection.Close();
                    Connection.Dispose();
                    MessageChannel.Dispose();
                }
                disposed = true;
                Dispose();
            }
        }
        public abstract void Serve(ITaskContext context, AsyncCallHandle handle);
        public abstract void Restart(ITaskContext context, AsyncCallHandle handle);
        public abstract void Shutdown(ITaskContext context, AsyncCallHandle handle);
    }
}

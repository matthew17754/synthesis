using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Util;
using System;
using System.Net;

namespace SynthesisMultiplayer.Server.UDP
{
    public class ClientListener : ManagedUDPTask
    {
        public ClientListener(
            Channel<(string, AsyncCallHandle)> statusChannel, 
            Channel<(string, AsyncCallHandle)> messageChannel, 
            IPAddress ip, 
            int port = 33000) :
            base(ip, port)
        {
        }

        public override void Restart(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }

        public override void Serve(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }

        public override void Shutdown(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }
    }
}

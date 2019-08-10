using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Server.UDP
{
    class ClientConnection : ManagedUDPTask
    {
        public ClientConnection
            (Channel<(string, AsyncCallHandle)> statusChannel, 
            Channel<(string, AsyncCallHandle)> messageChannel,
            IPAddress ip, 
            int port = 33000) : 
            base(ip, port)
        {
        }

        [Callback(methodName: Methods.Server.Restart)]
        public override void Restart(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }

        [Callback(methodName: Methods.Server.Serve)]
        public override void Serve(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }

        [Callback(methodName: Methods.Server.Shutdown)]
        public override void Shutdown(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }
    }
}

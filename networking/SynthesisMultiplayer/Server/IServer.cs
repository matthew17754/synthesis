using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Server;
using SynthesisMultiplayer.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Common
{
    public interface IServer
    {
        void Serve(ITaskContext context, AsyncCallHandle handle);
        void Restart(ITaskContext context, AsyncCallHandle handle);
        void Shutdown(ITaskContext context, AsyncCallHandle handle);
    }
}

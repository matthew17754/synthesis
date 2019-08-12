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

    public partial class Methods
    {
        public static class Server
        {
            public const string Serve = "SERVE";
            public const string Restart = "RESTART";
            public const string Shutdown = "SHUTDOWN";

        }
    }

    public interface IServer : IManagedTask
    {
        void ServeCallback(ITaskContext context, AsyncCallHandle handle);
        void RestartCallback(ITaskContext context, AsyncCallHandle handle);
        void ShutdownCallback(ITaskContext context, AsyncCallHandle handle);
    }
    public static class IServerMethods
    {
        public static void Serve(this IServer server, params dynamic[] args)
        {
            server.Do(Methods.Server.Serve, args: args).Wait();
        }
        public static void Restart(this IServer server, params dynamic[] args)
        {
            server.Do(Methods.Server.Restart, args: args).Wait();
        }
        public static void Shutdown(this IServer server, params dynamic[] args)
        {
            server.Do(Methods.Server.Shutdown, args: args).Wait();
        }
    }
}

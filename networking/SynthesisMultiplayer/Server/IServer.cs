using Multiplayer.Attribute;
using Multiplayer.Server;
using Multiplayer.Actor;
using Multiplayer.Actor.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Multiplayer.Common;
namespace Multiplayer.Common
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

}
namespace Multiplayer.Server {
    public interface IServer : IActor
    {
        void ServeCallback(ITaskContext context, ActorCallbackHandle handle);
        void RestartCallback(ITaskContext context, ActorCallbackHandle handle);
        void ShutdownCallback(ITaskContext context, ActorCallbackHandle handle);
    }
    public static class IServerMethods
    {
        public static void Serve(this IServer server, params dynamic[] args)
        {
            server.Call(Methods.Server.Serve, args: args);
        }
        public static void Restart(this IServer server, params dynamic[] args)
        {
            server.Call(Methods.Server.Restart, args: args);
        }
        public static void Shutdown(this IServer server, params dynamic[] args)
        {
            server.Call(Methods.Server.Shutdown, args: args);
        }
    }
}

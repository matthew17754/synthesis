using Multiplayer.Attribute;
using Multiplayer.Common;
using Multiplayer.Server.gRPC;
using Multiplayer.Server.UDP;
using Multiplayer.Threading;
using Multiplayer.Threading.Runtime;
using Multiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Multiplayer.Threading.ManagedTaskHelper;
using static Multiplayer.Threading.Runtime.ArgumentPacker;
namespace Multiplayer.Service
{
    public class LobbyService : IManagedTask, IServer
    {

        public bool Initialized { get; private set; }
        public bool Alive { get; private set; }
        public Guid Id { get; private set; }
        public ManagedTaskStatus Status { get; private set; }

        protected Guid Broadcaster, ConnectionListener, FanoutService, Lobby;
        protected bool Serving = false;
        protected int GrpcPort;
        protected int ListenerPort;
        protected int LobbyPort;
        protected int BroadcastPort;
        public LobbyService(int broadcastPort = 33002, int grpcPort = 33005, int listenerPort = 33006, int lobbyPort = 33007)
        {
            BroadcastPort = broadcastPort;
            GrpcPort = grpcPort;
            ListenerPort = listenerPort;
            LobbyPort = lobbyPort;
        }

        public void Dispose()
        {
        }

        public void Initialize(Guid id)
        {
            Broadcaster = Start(new LobbyBroadcaster(BroadcastPort, "test", 12));
            ConnectionListener = Start(new ConnectionListener(ListenerPort));
            Lobby = Start(new LobbyHandler(ConnectionListener, GrpcPort));
            FanoutService = Start(new FanoutService(LobbyPort));
            Id = id;
            Status = ManagedTaskStatus.Initialized;
            Initialized = true;
            Alive = true;
        }

        public void Loop()
        {
            if (Serving)
            {
                var newConnection = (Optional<Guid>)Call(Lobby, Methods.LobbyHandler.GetLatestConnection).Result;
                if (newConnection.Valid)
                {
                    var connectionInfo = ((ConnectionListener)GetTask(ConnectionListener)).GetConnectionInfo(newConnection);
                    ((FanoutService)GetTask(FanoutService)).AddConnection(connectionInfo.Address, connectionInfo.Port);
                }
            }
        }

        public void Terminate(string reason = null, params dynamic[] args)
        {
            this.Shutdown();
        }

        [Callback(name: Methods.Server.Serve, argNames: "test")]
        public void ServeMethod(ITaskContext context, AsyncCallHandle handle)
        {
            ((LobbyBroadcaster)GetTask(Broadcaster)).Serve();
            ((ConnectionListener)GetTask(ConnectionListener)).Serve();
            ((LobbyHandler)GetTask(Lobby)).Serve();
            Serving = true;
            handle.Done();
        }

        [Callback(name: Methods.Server.Restart)]
        public void RestartMethod(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }

        [Callback(Methods.Server.Shutdown)]
        public void ShutdownMethod(ITaskContext context, AsyncCallHandle handle)
        {
            Serving = false;
            ManagedTaskHelper.Terminate(Lobby, "Lobby Shutdown");
            ManagedTaskHelper.Terminate(FanoutService, "Lobby Shutdown");
            ManagedTaskHelper.Terminate(ConnectionListener, "Lobby Shutdown");
            ManagedTaskHelper.Terminate(Broadcaster, "Lobby Shutdown");
            Alive = false;
            Initialized = false;
            Status = ManagedTaskStatus.Completed;
            Thread.Sleep(50);
            handle.Done();
            Thread.Sleep(50);
        }
    }
}

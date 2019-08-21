using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Server.gRPC;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SynthesisMultiplayer.Threading.ManagedTaskHelper;
namespace SynthesisMultiplayer.Service
{
    public class LobbyService : IManagedTask, IServer
    {

        public bool Initialized { get; private set; }
        public bool Alive { get; private set; }
        public Guid Id { get; private set; }
        public ManagedTaskStatus Status { get; private set; }

        protected Guid Broadcaster, ConnectionListener, FanoutService, Lobby;
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
            var newConnection = (Optional<Guid>)Call(Lobby, Methods.LobbyHandler.GetLatestConnection).Result;
            if(newConnection.Valid)
            {
                var connectionInfo = ((ConnectionListener)GetTask(ConnectionListener)).GetConnectionInfo(newConnection);
                ((FanoutService)GetTask(FanoutService)).AddListener(connectionInfo.Address, connectionInfo.Port);
            }
        }

        public void Terminate(string reason = null, params dynamic[] args)
        {
            this.Call(Methods.Server.Shutdown).Wait();
        }

        [Callback(methodName: Methods.Server.Serve)]
        public void ServeCallback(ITaskContext context, AsyncCallHandle handle)
        {
            ((LobbyBroadcaster)GetTask(Broadcaster)).Serve();
            ((ConnectionListener)GetTask(ConnectionListener)).Serve();
            ((LobbyHandler)GetTask(Lobby)).Serve();
            handle.Done();
        }

        [Callback(methodName: Methods.Server.Restart)]
        public void RestartCallback(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }

        [Callback(methodName: Methods.Server.Shutdown)]
        public void ShutdownCallback(ITaskContext context, AsyncCallHandle handle)
        {
            ManagedTaskHelper.Terminate(Broadcaster);
            ManagedTaskHelper.Terminate(ConnectionListener);
            ManagedTaskHelper.Terminate(FanoutService);
            ManagedTaskHelper.Terminate(Lobby);
            handle.Done();
        }
    }
}

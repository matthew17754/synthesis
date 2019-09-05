using Multiplayer.Actor;
using Multiplayer.Actor.Runtime;
using Multiplayer.Attribute;
using Multiplayer.Client.gRPC;
using Multiplayer.Client.UDP;
using Multiplayer.Collections;
using Multiplayer.Common;
using Multiplayer.Common.UDP;
using Multiplayer.IO;
using Multiplayer.IPC;
using Multiplayer.Server;
using System;
using System.Net;
using static Multiplayer.Actor.ActorHelper;
using static Multiplayer.Actor.Runtime.ArgumentUnpacker;

namespace Multiplayer.Common
{
    public partial class Methods
    {
        public class LobbyClientService
        {
            public const string Connect = "CONNECT";
        }
    }
}

namespace Multiplayer.Service
{
    public class LobbyClientService : IActor
    {
        public bool Initialized { get; private set; }
        public bool Alive { get; private set; }
        public Guid Id { get; private set; }
        public ManagedTaskStatus Status { get; private set; }

        protected Channel<Either<string, byte[]>> Data;

        protected Guid LobbyListener, LobbyClient;
        protected Guid StreamListener, StreamSender;

        protected bool Connected { get; private set; }

        protected IPEndPoint lobbyEndpoint;

        private bool disposedValue = false;

        public void Initialize(Guid id)
        {
            Id = id;
            Initialized = true;

            LobbyListener = Start(new LobbyBroadcastListener());
            LobbyClient = Start(new LobbyClient());
            Alive = true;
        }

        public void Loop()
        {
        }

        public void Terminate(string reason = null, params dynamic[] args)
        {
            Info.Log("Shutting lobby client");
            Initialized = false;
            Alive = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void JoinLobby(IPEndPoint endpoint = null)
        {
            lobbyEndpoint = endpoint ?? lobbyEndpoint;
            Call(LobbyClient, Methods.LobbyClient.JoinLobby, lobbyEndpoint);
        }
        public void JoinLobby(string Ip)
        {
            lobbyEndpoint = new IPEndPoint(IPAddress.Parse(
                    Ip.Substring(0, Ip.IndexOf(':'))),
                    int.Parse(Ip.Substring(Ip.IndexOf(':') + 1)));
            JoinLobby();
        }

        [Callback(Methods.LobbyClientService.Connect, "ip", "lobbyPort")]
        [Argument("ip", typeof(string))]
        [Argument("lobbyPort", typeof(int), 33000, ActorCallbackArgumentAttributes.HasDefault)]
        public void ConnectMethod(ITaskContext context, ActorCallbackHandle handle)
        {
            var (ip, lobbyPort) = GetArgs<string, int>(handle);
            JoinLobby(ip);
            StreamSender = Start(new StreamSender(lobbyEndpoint.Address, ((LobbyClient)GetTask(LobbyClient)).RemotePort));
            StreamListener = Start(new StreamListener(lobbyEndpoint.Address, ((LobbyClient)GetTask(LobbyClient)).LocalPort));
            ((StreamSender)GetTask(StreamSender)).Serve();
            ((StreamListener)GetTask(StreamListener)).Serve();
            while (!((StreamSender)GetTask(StreamSender)).Initialized && !((StreamListener)GetTask(StreamListener)).Initialized) { }
            Connected = true;
            handle.Done();
        }

        [Callback("send", "data")]
        [Argument("data", typeof(string))]
        public void SendCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            var data = GetArgs<string>(handle);
            Debug.Log("Sending " + data);
            ((StreamSender)GetTask(StreamSender)).Send(data);
            handle.Done();
        }
        public void Connect(string ip) =>
            this.Call(Methods.LobbyClientService.Connect, ip);
    }
}

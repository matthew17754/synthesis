using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Client.gRPC;
using SynthesisMultiplayer.Client.UDP;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Common.UDP;
using SynthesisMultiplayer.IO;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Threading.Runtime;
using SynthesisMultiplayer.Util;
using System;
using System.Net;
using static SynthesisMultiplayer.Threading.ManagedTaskHelper;
using static SynthesisMultiplayer.Threading.Runtime.ArgumentPacker;

namespace SynthesisMultiplayer.Common
{
    public partial class Methods
    {
        public class LobbyClientService
        {
            public const string Connect = "CONNECT";
        }
    }
}

namespace SynthesisMultiplayer.Service
{
    public class LobbyClientService : IManagedTask
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
            this.Call(Methods.Server.Shutdown).Wait();
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
            Call(LobbyClient, Methods.LobbyClient.JoinLobby, lobbyEndpoint).Wait();
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
        [Argument("lobbyPort", typeof(int), 33000, RuntimeArgumentAttributes.HasDefault)]
        public void ConnectMethod(ITaskContext context, AsyncCallHandle handle)
        {
            var (ip, lobbyPort) = GetArgs<string, int>(handle);
            JoinLobby(ip);
            StreamSender = Start(new StreamSender(lobbyEndpoint.Address, lobbyPort+1));
            StreamListener = Start(new StreamListener(lobbyEndpoint.Address, lobbyPort));
            ((StreamSender)GetTask(StreamSender)).Serve();
            ((StreamListener)GetTask(StreamListener)).Serve();
            while(!((StreamSender)GetTask(StreamSender)).Initialized && !((StreamListener)GetTask(StreamListener)).Initialized) { }
            Connected = true;
            handle.Done();
        }

        [Callback("send", "data")]
        [Argument("data", typeof(string))]
        public void SendCallback(ITaskContext context, AsyncCallHandle handle)
        {
            var data = GetArgs<string>(handle);
            Debug.Log("Sending " + data);
            ((StreamSender)GetTask(StreamSender)).Send(data);
            handle.Done();
        }
        public void Connect(string ip) =>
            this.Call(Methods.LobbyClientService.Connect, ip).Wait();
    }
}

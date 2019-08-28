using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Client.gRPC;
using SynthesisMultiplayer.Client.UDP;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Threading.Runtime;
using SynthesisMultiplayer.Util;
using System;
using System.Net;
using static SynthesisMultiplayer.Threading.ManagedTaskHelper;

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

        [Callback(name: Methods.LobbyClientService.Connect)]
        public void ConnectMethod(ITaskContext context, AsyncCallHandle handle)
        {
            var ip = handle.Arguments.Dequeue();
        }
    }
}

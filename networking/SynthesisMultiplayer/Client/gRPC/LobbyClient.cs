using Google.Protobuf;
using MatchmakingService;
using Multiplayer.Attribute;
using Multiplayer.Common;
using Multiplayer.IO;
using Multiplayer.Actor;
using Multiplayer.Actor.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Multiplayer.Common
{
    public partial class Methods
    {
        public class LobbyClient
        {
            public const string JoinLobby = "JOIN_LOBBY";
            public const string JoinLobbyStatus = "JOIN_LOBBY_STATUS";

            public const string RejoinLobby = "REJOIN_LOBBY";
            public const string RejoinLobbyStatus = "REJOIN_LOBBY_STATUS";

            public const string Disconnect = "DISCONNECT";
        }
    }
}

namespace Multiplayer.Client.gRPC
{
    public class LobbyClient : IActor
    {
        public bool Connected { get; private set; }
        public bool Initialized { get; private set; }
        public bool Alive { get; private set; }
        public Guid Id { get; private set; }
        public ManagedTaskStatus Status { get; private set; }
        public LobbyClient() { }

        [Callback(Methods.LobbyClient.JoinLobby, "endpoint", "timeout")]
        [Argument("endpoint", typeof(IPEndPoint))]
        [Argument("timeout", typeof(int), -1, ActorCallbackArgumentAttributes.HasDefault, ActorCallbackArgumentAttributes.Optional)]
        public void JoinLobbyCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            (IPEndPoint endpoint, int timeout) = ArgumentUnpacker.GetArgs<IPEndPoint, int>(handle);
            var Channel = new Grpc.Core.Channel(endpoint.ToString(), Grpc.Core.ChannelCredentials.Insecure);
            var Client = new ServerHost.ServerHostClient(Channel);
            var res = Client.JoinLobby(new JoinLobbyRequest
            {
                Api = "v1"
            });
            if (string.IsNullOrEmpty(res.JobId))
            {
                throw new Exception("No job ID found");
            }
            using (var call = Client.JoinLobbyStatus())
            {
                var udpClient = new UdpClient();
                udpClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 33006));
                var message = new UDPValidatorMessage
                {
                    Api = "v1",
                    JobId = res.JobId
                };
                var outputStream = new MemoryStream();
                message.WriteTo(outputStream);
                outputStream.Position = 0;
                var outputData = new StreamReader(outputStream).ReadToEnd();
                var asyncHandle = new ActorCallbackHandle();
                var cancellationToken = new CancellationTokenSource();
                var responseTask = Task.Factory.StartNew(token =>
                {
                    while (!((CancellationToken)token).IsCancellationRequested && call.ResponseStream.MoveNext().Result)
                    {
                        udpClient.Send(Encoding.ASCII.GetBytes(outputData), outputData.Length);
                        var resp = call.ResponseStream.Current;
                        if (resp.Status == JoinLobbyStatusResponse.Types.Status.Connected)
                        {
                            break;
                        }
                        if (resp.ErrorCode != JoinLobbyStatusResponse.Types.ErrorCode.None)
                        {
                            throw new Exception("Failed to connect: internal error");
                        }
                    }
                    asyncHandle.Done();
                }, cancellationToken.Token);
                var deadline = timeout != -1 ? DateTime.Now.AddSeconds(timeout) : DateTime.Now.AddMinutes(5);
                while (!asyncHandle.Ready)
                {
                    if (DateTime.Now <= deadline)
                    {
                        call.RequestStream.WriteAsync(new JoinLobbyStatusRequest
                        {
                            Api = "v1",
                            JobId = res.JobId,
                        }).Wait();

                        Thread.Sleep(50);
                    } else
                    {
                        Connected = false;
                        cancellationToken.Cancel();
                        Info.Log("Failed to connect within time limit");
                        handle.Done();
                        return;
                    }
                }
                Info.Log($"Joined lobby on {endpoint.ToString()}");
                call.RequestStream.CompleteAsync();
                responseTask.Wait();
                Channel.ShutdownAsync().Wait();
                Connected = true;
                Channel = null;
                Client = null;
                handle.Done();
            }
        }
        public void JoinLobby(IPEndPoint endpoint) =>
            this.Call(Methods.LobbyClient.JoinLobby, endpoint).Wait();
         public void JoinLobby(string Ip) =>
            this.Call(Methods.LobbyClient.JoinLobby, 
                new IPEndPoint(IPAddress.Parse(
                    Ip.Substring(0, Ip.IndexOf(':'))), 
                    int.Parse(Ip.Substring(Ip.IndexOf(':')))))
            .Wait();
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Initialize(Guid id)
        {
            Id = id;
            Initialized = true;
            Alive = true;
            Status = ManagedTaskStatus.Initialized;
        }

        public void Loop()
        {
        }

        public void Terminate(string reason = null, params dynamic[] args)
        {
            Initialized = false;
            Alive = false;
        }
    }
}

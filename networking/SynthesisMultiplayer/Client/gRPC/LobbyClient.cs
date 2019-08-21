using Google.Protobuf;
using MatchmakingService;
using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Common
{
    public partial class Methods
    {
        public class LobbyClient
        {
            public const string JoinLobby = "JOIN_LOBBY";
            public const string JoinLobbyStatus = "JOIN_LOBBY_STATUS";
        }
    }
}

namespace SynthesisMultiplayer.Client.gRPC
{
    public class LobbyClient : IManagedTask
    {
        public bool Initialized { get; private set; }
        public bool Alive { get; private set; }
        public Guid Id { get; private set; }
        public ManagedTaskStatus Status { get; private set; }
        public Grpc.Core.Channel Channel;
        protected IPEndPoint Endpoint;
        protected MatchmakingService.ServerHost.ServerHostClient Client;
        public LobbyClient(IPEndPoint lobbyAddress)
        {
            Endpoint = lobbyAddress;
        }

        [Callback(methodName: Methods.LobbyClient.JoinLobby)]
        public void JoinLobbyCallback(ITaskContext context, AsyncCallHandle handle)
        {
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
                var asyncHandle = new AsyncCallHandle();
                var responseTask = Task.Factory.StartNew(() =>
                {
                    while (call.ResponseStream.MoveNext().Result)
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
                });

                while (!asyncHandle.Ready)
                {
                    call.RequestStream.WriteAsync(new JoinLobbyStatusRequest
                    {
                        Api = "v1",
                        JobId = res.JobId,
                    }).Wait();
                    Thread.Sleep(50);
                }
                Console.WriteLine("Done");
                call.RequestStream.CompleteAsync();
                responseTask.Wait();
                handle.Done();
            }
        }
        public void JoinLobby()
        {
            this.Call(Methods.LobbyClient.JoinLobby).Wait();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Initialize(Guid id)
        {
            Id = id;
            Channel = new Grpc.Core.Channel(Endpoint.ToString(), Grpc.Core.ChannelCredentials.Insecure);
            Client = new MatchmakingService.ServerHost.ServerHostClient(Channel);
            Initialized = true;
            Alive = true;
            Status = ManagedTaskStatus.Initialized;
        }

        public void Loop()
        {
        }

        public void Terminate(string reason = null, params dynamic[] args)
        {
            Client = null;
            Channel.ShutdownAsync().Wait();
        }
    }
}

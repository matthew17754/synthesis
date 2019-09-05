using Grpc.Core;
using MatchmakingService;
using Multiplayer.Actor;
using Multiplayer.Actor.Runtime;
using Multiplayer.Attribute;
using Multiplayer.Common;
using Multiplayer.IO;
using Multiplayer.IPC;
using Multiplayer.Server.UDP;
using Multiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Multiplayer.Actor.ActorHelper;

namespace Multiplayer.Common
{
    public partial class Methods
    {
        public class LobbyHandler
        {
            public const string GetLatestConnection = "GET_LATEST_CONNECTION";
        }
    }
}

namespace Multiplayer.Server.gRPC
{
    public class LobbyHandler : IActor, IServer
    {
        private class Job
        {
            public enum Status
            {
                Started,
                Running,
                Complete,
                Errored
            }
            public Guid JobId;
            public Status JobStatus;
            public IPEndPoint Ip;
            public int LocalPort, RemotePort;
        }

        public bool Initialized { get; private set; }
        public bool Alive { get; private set; }
        public Guid Id { get; private set; }
        public ManagedTaskStatus Status { get; private set; }
        protected Grpc.Core.Server Server;
        protected Guid ListenerPid;
        protected int Port;
        protected Channel<(Guid, int, int)> CompletedJobs;
        public LobbyHandler(Guid listenerPid, int port = 33002)
        {
            ListenerPid = listenerPid;
            Port = port;
            CompletedJobs = new Channel<(Guid, int, int)>();
        }

        private class LobbyGrpcServer : ServerHost.ServerHostBase
        {
            Guid ListenerPid;
            Dictionary<Guid, Job> Jobs;
            Channel<(Guid, int, int)> CompletedJobs;
            public LobbyGrpcServer(Guid listener, Channel<(Guid, int, int)> completedJobs)
            {
                ListenerPid = listener;
                Jobs = new Dictionary<Guid, Job>();
                CompletedJobs = completedJobs;
            }

            public override Task<JoinLobbyResponse> JoinLobby(JoinLobbyRequest req, ServerCallContext context)
            {
                if (req.BestPort == 0)
                    new Exception("No best port provided");
                var job = new Job
                {
                    JobId = Guid.NewGuid(),
                    JobStatus = Job.Status.Started,
                    Ip = new IPEndPoint(IPAddress.Parse(context.Host.Split(':')[0]), int.Parse(context.Host.Split(':')[1])),
                    LocalPort = PortUtils.GetAvailablePort(33010, 
                        context.Peer.Substring(5, context.Peer.LastIndexOf(':')-5) == "127.0.0.1" ? req.BestPort : 0),
                    RemotePort = req.BestPort
                };
                Jobs.Add(job.JobId, job);
                Info.Log("New join request");
                return Task.FromResult(new JoinLobbyResponse
                {
                    Api = "v1",
                    JobId = job.JobId.ToString(),
                    BestPort = job.LocalPort,           
                });
            }

            public override async Task JoinLobbyStatus(IAsyncStreamReader<JoinLobbyStatusRequest> requestStream, IServerStreamWriter<JoinLobbyStatusResponse> responseStream, ServerCallContext context)
            {
                while (await requestStream.MoveNext())
                {
                    var listener = (ConnectionListener)GetTask(ListenerPid);
                    var connInfo = listener.GetConnectionInfo(new Guid(requestStream.Current.JobId));
                    if (connInfo != null)
                    {
                        if (connInfo.Address != null)
                        {
                            await responseStream.WriteAsync(new JoinLobbyStatusResponse
                            {
                                Api = "v1",
                                Status = JoinLobbyStatusResponse.Types.Status.Connected
                            });
                            CompletedJobs.Send((new Guid(requestStream.Current.JobId), 
                                Jobs[new Guid(requestStream.Current.JobId)].LocalPort,
                                Jobs[new Guid(requestStream.Current.JobId)].RemotePort));
                        }
                    }
                    else
                    {
                        await responseStream.WriteAsync(new JoinLobbyStatusResponse
                        {
                            Api = "v1",
                            Status = JoinLobbyStatusResponse.Types.Status.Connecting
                        });
                        Thread.Sleep(50);
                    }
                }
            }
        }

        [Callback(name: Methods.LobbyHandler.GetLatestConnection)]
        public void GetLatestConnectionMethod(ITaskContext context, ActorCallbackHandle handle) =>
            handle.Result = CompletedJobs.TryGet();
        [Callback(name: Methods.Server.Serve)]
        public void ServeCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            Server = new Grpc.Core.Server
            {
                Services = { ServerHost.BindService(new LobbyGrpcServer(ListenerPid, CompletedJobs)) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            Server.Start();
            Info.Log("Serving Grpc");
            handle.Done();
        }

        [Callback(name: Methods.Server.Restart)]
        public void RestartCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            throw new NotImplementedException();
        }

        [Callback(name: Methods.Server.Shutdown)]
        public void ShutdownCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            Server.KillAsync().Wait();
            Alive = false;
            Initialized = false;
            Server = null;
            handle.Done();
        }
        public void Initialize(Guid taskId)
        {
            Id = taskId;
            Status = ManagedTaskStatus.Initialized;
            Initialized = true;
            Alive = true;
        }

        public void Terminate(string reason = null, params dynamic[] args)
        {
            this.Call(Methods.Server.Shutdown).Wait();
        }

        public void Loop() { }

        public void Dispose()
        {
        }
    }
}

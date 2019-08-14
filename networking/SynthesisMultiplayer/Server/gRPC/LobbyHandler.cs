using System.Net.Sockets;
using MatchmakingService;
using System.Collections.Generic;
using System;
using Grpc;
using System.Threading.Tasks;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;
using static SynthesisMultiplayer.Threading.ManagedTaskHelper;
using Grpc.Core;
using System.Threading;

namespace SynthesisMultiplayer.Server.gRPC
{
    class LobbyHandler : IManagedTask
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
            public Status jobStatus;
            public Uri uri;
        }

        private class LobbyGrpcServer : ServerHost.ServerHostBase
        {
            Guid ListenerPid;
            Dictionary<Guid, Job> Jobs;

            public LobbyGrpcServer(Guid listener)
            {
                ListenerPid = listener;
                Jobs = new Dictionary<Guid, Job>();
            }

            public override Task<JoinLobbyResponse> JoinLobby(JoinLobbyRequest req, ServerCallContext context)
            {
                var job = new Job
                {
                    JobId = new Guid(),
                    jobStatus = Job.Status.Started,
                    uri = new Uri(context.Host)
                };
                Jobs.Add(job.JobId, job);
                return Task.FromResult(new JoinLobbyResponse
                {
                    Api = "v1",
                    JobId = job.JobId.ToString()
                });
            }

            public override async Task JoinLobbyStatus(IAsyncStreamReader<JoinLobbyStatusRequest> requestStream, IServerStreamWriter<JoinLobbyStatusResponse> responseStream, ServerCallContext context)
            {
                while (await requestStream.MoveNext())
                {
                    var listener = (ConnectionListener) GetTask(ListenerPid);
                    var connInfo = listener.GetConnectionInfo(new Guid(requestStream.Current.JobId));
                    if (connInfo != null)
                    {
                        if (connInfo.Address == null)
                        {
                            await responseStream.WriteAsync(new JoinLobbyStatusResponse
                            {
                                Api = "v1",
                            });
                        }
                        Thread.Sleep(50);
                    }
                }
            }
        }

        public bool Alive => throw new NotImplementedException();

        public bool Initialized => throw new NotImplementedException();

        public ManagedTaskStatus Status => throw new NotImplementedException();

        public Guid Id => throw new NotImplementedException();

        public void Initialize(Guid taskId)
        {
            throw new NotImplementedException();
        }

        public void Terminate(string reason = null, params dynamic[] args)
        {
            throw new NotImplementedException();
        }

        public void Loop() { }

        public void Dispose()
        {

        }
    }
}

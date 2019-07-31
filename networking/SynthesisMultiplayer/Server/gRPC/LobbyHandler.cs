using System.Net.Sockets;
using MatchmakingService;
using System.Collections.Generic;
using System;
using Grpc;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Server.gRPC 
{
    class LobbyHandler : ServerHost.ServerHostBase
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

        Dictionary<int, Common.ConnectionInfo> connections;
        Dictionary<Guid, Job> jobs;

        public override Task<JoinLobbyResponse> JoinLobby(JoinLobbyRequest req, Grpc.Core.ServerCallContext context)
        {
            var job = new Job();
            job.JobId = new Guid();
            job.jobStatus = Job.Status.Started;
            job.uri = new Uri(context.Host);
            return Task.FromResult(new JoinLobbyResponse {
                Api = "v1",
                JobId = job.JobId.ToString()
            });
        }
    }
}

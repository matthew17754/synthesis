﻿using System.Net.Sockets;
using MatchmakingService;
using System.Collections.Generic;
using System;
using Grpc;
using System.Threading.Tasks;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;

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
            ConnectionListener ConnectionListener;
            Dictionary<Guid, Job> Jobs;

            public LobbyGrpcServer(ConnectionListener listener)
            {
                ConnectionListener = listener;
                Jobs = new Dictionary<Guid, Job>();
            }

            public override Task<JoinLobbyResponse> JoinLobby(JoinLobbyRequest req, Grpc.Core.ServerCallContext context)
            {
                var job = new Job();
                job.JobId = new Guid();
                job.jobStatus = Job.Status.Started;
                job.uri = new Uri(context.Host);
                Jobs.Add(job.JobId, job);
                return Task.FromResult(new JoinLobbyResponse
                {
                    Api = "v1",
                    JobId = job.JobId.ToString()
                });
            }
        }

        Dictionary<string, IManagedTask> Children;
        LobbyGrpcServer Server;

        public bool Alive => throw new NotImplementedException();

        public bool Initialized => throw new NotImplementedException();

        public ManagedTaskStatus Status => throw new NotImplementedException();

        public (IManagedTask, Task) GetChild(string name)
        {
            throw new NotImplementedException();
        }

        public void RestartChild(string name)
        {
            throw new NotImplementedException();
        }

        public string SpawnChild(IManagedTask taskObject, string name = "")
        {
            throw new NotImplementedException();
        }

        public void SendMessage((string, AsyncCallHandle) message)
        {
            throw new NotImplementedException();
        }

        public Util.Optional<(string, AsyncCallHandle)> GetMessage()
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Terminate(string reason = null, Dictionary<string, dynamic> state = null)
        {
            throw new NotImplementedException();
        }

        public void Loop()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

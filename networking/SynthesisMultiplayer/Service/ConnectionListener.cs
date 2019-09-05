using MatchmakingService;
using Multiplayer.Actor;
using Multiplayer.Actor.Runtime;
using Multiplayer.Attribute;
using Multiplayer.Common;
using Multiplayer.IO;
using Multiplayer.IPC;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static Multiplayer.Actor.Runtime.ArgumentUnpacker;

namespace Multiplayer.Common
{
    public partial class Methods
    {
        public class ConnectionListener
        {
            public const string GetConnectionInfo = "GET_CONNECTION_INFO";
        }
    }
}

namespace Multiplayer.Server.UDP
{
    public class ConnectionListener : ManagedUdpTask
    {
        protected class ConnectionListenerClient : TaskContext
        {
            public UdpClient client;
            public IPEndPoint peer;

            public Channel<byte[]> sender;
        }
        private class ConnectionListenerData
        {
            public ConnectionListenerData()
            {
                Mutex = new Mutex();
                ConnectionInfo = new Dictionary<Guid, IPEndPoint>();
            }
            public Mutex Mutex;
            public Dictionary<Guid, IPEndPoint> ConnectionInfo;
            public IPEndPoint LastEndpoint;
        }
        [SavedStateAttribute]
        ConnectionListenerData ServerData;
        bool disposed;
        Channel<byte[]> Channel;
        bool initialized { get; set; }
        bool Serving { get; set; }

        public override bool Alive => initialized; 
        public override bool Initialized => initialized;

        public ConnectionListener(int port = 33000) :
            base(IPAddress.Any, port)
        {
            Serving = false;
        }
        private void ReceiveMethod(IAsyncResult result)
        {
            if (Serving)
            {
                var context = ((ConnectionListenerClient)(result.AsyncState));
                var udpClient = context.client;
                var peer = context.peer;
                var receivedData = udpClient.EndReceive(result, ref context.peer);
                ServerData.LastEndpoint = context.peer;
                context.sender.Send(receivedData);
                Debug.Log("Got Data '" + Encoding.Default.GetString(receivedData) + "'");
                context.peer = new IPEndPoint(IPAddress.Any, Endpoint.Port);
                udpClient.BeginReceive(ReceiveMethod, context);
            }
        }
        public IPEndPoint GetConnectionInfo(Guid id) =>
            this.Call(Methods.ConnectionListener.GetConnectionInfo, id);
        public override void Loop()
        {
            if (Serving)
            {
                var newData = Channel.TryGet();
                if (!newData.Valid)
                {
                    return;
                }
                try
                {
                    var decodedData = UDPValidatorMessage.Parser.ParseFrom(newData);
                    if (decodedData.Api != "v1")
                    {
                        Warning.Log("API version not recognized. Skipping");
                        return;
                    }
                    ServerData.ConnectionInfo[new Guid(decodedData.JobId)] = ServerData.LastEndpoint;
                }
                catch (Exception)
                {
                    Warning.Log("API version not recognized. Skipping");
                    return;
                }
            }
            else
            {
                Thread.Sleep(50);
            }
        }

        [Callback(name: Methods.Server.Serve)]
        public override void ServeCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            Info.Log("Listener started");
            Connection.BeginReceive(ReceiveMethod, new ConnectionListenerClient
            {
                client = Connection,
                peer = Endpoint,
                sender = Channel,
            });
            Serving = true;
            handle.Done();
        }

        [Callback(name: Methods.Server.Shutdown)]
        public override void ShutdownCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            Info.Log("Shutting down listener");
            Serving = false;
            initialized = false;
            Connection.Close();
            Status = ManagedTaskStatus.Completed;
            handle.Done();
        }

        [Callback(name: Methods.Server.Restart)]
        public override void RestartCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            if(handle.Arguments.Dequeue() == true)
            {
                var state = StateBackup.DumpState(this);
                Terminate();
                Initialize(Id);
                StateBackup.RestoreState(this, state);
            }
            Terminate();
            Initialize(Id);
            handle.Done();

        }

        [Callback(Methods.ConnectionListener.GetConnectionInfo, "jobId")]
        [Argument("jobId", typeof(Guid))]
        public void GetConnectionInfoMethod(ITaskContext context, ActorCallbackHandle handle)
        {
            var jobId = GetArgs<Guid>(handle);
            lock (ServerData.Mutex)
                handle.Result = ServerData.ConnectionInfo.ContainsKey(jobId) ? ServerData.ConnectionInfo[jobId] : null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Connection.Close();
                    Connection.Dispose();
                    Channel.Dispose();
                }
                disposed = true;
                Serving = false;
                Dispose();
            }
        }

        public override void Initialize(Guid taskId)
        {
            Id = taskId;
            initialized = true;
            ServerData = new ConnectionListenerData();
            Channel = new Channel<byte[]>();
            Connection = new UdpClient(Endpoint);
        }

        public override void Terminate(string reason = null, params dynamic[] args)
        {
            this.Call(Methods.Server.Shutdown);
            Info.Log("Server closed: '" + (reason ?? "No reason provided") + "'");
        }
    }
}

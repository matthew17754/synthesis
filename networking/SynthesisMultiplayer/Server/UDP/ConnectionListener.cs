using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Util;
using MatchmakingService;
using System.Text;
using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Threading.Methods;

namespace SynthesisMultiplayer.Server.UDP
{
    public class ConnectionListener : ManagedUDPTask
    {
        protected class ConnectionListenerContext : TaskContext
        {
            public UdpClient client;
            public IPEndPoint peer;

            public Channel<byte[]> sender;
        }
        private class ListenerServerData
        {
            public ListenerServerData()
            {
                Mutex = new Mutex();
                ConnectionInfo = new Dictionary<Guid, IPEndPoint>();
            }
            public Mutex Mutex;
            public Dictionary<Guid, IPEndPoint> ConnectionInfo;
            public IPEndPoint LastEndpoint;
        }
        [SavedState]
        ListenerServerData ServerData;
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
        private void ReceiveCallback(IAsyncResult result)
        {
            if (Serving)
            {
                var context = ((ConnectionListenerContext)(result.AsyncState));
                var udpClient = context.client;
                var peer = context.peer;
                var receivedData = udpClient.EndReceive(result, ref context.peer);
                ServerData.LastEndpoint = context.peer;
                context.sender.Send(receivedData);
                Console.WriteLine("Got Data '" + Encoding.Default.GetString(receivedData) + "'");
                context.peer = new IPEndPoint(IPAddress.Any, Endpoint.Port);
                udpClient.BeginReceive(ReceiveCallback, context);
            }
        }
        private IPEndPoint GetConnectionInfo(Guid id)
        {
            lock (ServerData.Mutex)
                return ServerData.ConnectionInfo.ContainsKey(id) ? ServerData.ConnectionInfo[id] : null;
        }
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
                        Console.WriteLine("API version not recognized. Skipping");
                        return;
                    }
                    ServerData.ConnectionInfo[new Guid(decodedData.JobId)] = ServerData.LastEndpoint;
                }
                catch (Exception e)
                {
                    Console.WriteLine("API version not recognized. Skipping");
                    return;
                }
            }
            else
            {
                Thread.Sleep(50);
            }
        }

        [Callback(methodName: Methods.Server.Serve)]
        public override void Serve(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Listener started");
            Connection.BeginReceive(ReceiveCallback, new ConnectionListenerContext
            {
                client = Connection,
                peer = Endpoint,
                sender = Channel,
            });
            Serving = true;
            handle.Ready = true;
        }

        [Callback(methodName: Methods.Server.Shutdown)]
        public override void Shutdown(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Shutting down listener");
            Serving = false;
            initialized = false;
            handle.Ready = true;
            Status = ManagedTaskStatus.Completed;
            Dispose();
        }

        [Callback(methodName: Methods.Server.Restart)]
        public override void Restart(ITaskContext context, AsyncCallHandle handle)
        {
            var state = handle.Arguments.Dequeue();
            Dispose();
            Initialize();
            StateBackup.RestoreState(this, state);
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

        public override void Initialize()
        {
            initialized = true;
            ServerData = new ListenerServerData();
            Channel = new Channel<byte[]>();
            Connection = new UdpClient(Endpoint);
        }

        public override void Terminate(string reason = null, Dictionary<string, dynamic> state = null)
        {
            this.Call(Methods.Server.Shutdown).Wait();
            Console.WriteLine("Server closed: '" + (reason ?? "No reason provided") + "'");
        }
    }
}

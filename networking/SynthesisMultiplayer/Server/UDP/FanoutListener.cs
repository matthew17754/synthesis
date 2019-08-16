using MatchmakingService;
using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
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
        public class ClientListener
        {
            public const string GetClientData = "GET_CLIENT_DATA";
        }
    }
}

namespace SynthesisMultiplayer.Server.UDP
{
    public class FanoutListener : ManagedUdpTask
    {
        protected class ConnectionListenerContext : TaskContext
        {
            public UdpClient client;
            public IPEndPoint peer;

            public Channel<byte[]> sender;
        }
        private class ClientListenerData
        {
            public ClientListenerData()
            {
                Data = new Queue<byte[]>();
                Mutex = new Mutex();
            }
            public Mutex Mutex;
            public Queue<byte[]> Data;
            public IPEndPoint LastEndpoint;
        }
        [SavedState]
        ClientListenerData ClientData;
        bool disposed;
        Channel<byte[]> Channel;
        bool initialized { get; set; }
        bool Serving { get; set; }

        public override bool Alive => initialized;
        public override bool Initialized => initialized;

        public FanoutListener(int port = 33000) :
            base(IPAddress.Any, port)
        {
            Serving = false;
            ClientData = new ClientListenerData();
            Channel = new Channel<byte[]>();
        }
        private void ReceiveCallback(IAsyncResult result)
        {
            if (Serving)
            {
                var context = ((ConnectionListenerContext)(result.AsyncState));
                var udpClient = context.client;
                var peer = context.peer;
                var receivedData = udpClient.EndReceive(result, ref context.peer);
                ClientData.LastEndpoint = context.peer;
                context.sender.Send(receivedData);
                Console.WriteLine("Got Data '" + Encoding.Default.GetString(receivedData) + "'");
                context.peer = new IPEndPoint(IPAddress.Any, Endpoint.Port);
                udpClient.BeginReceive(ReceiveCallback, context);
            }
        }
        public IPEndPoint PeekClientData(Guid id) =>
            this.Call(Methods.ClientListener.GetClientData, false).Result;
        public IPEndPoint GetClientData(Guid id) =>
            this.Call(Methods.ClientListener.GetClientData, true).Result;

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
                    var decodedData = ClientDataFrame.Parser.ParseFrom(newData);
                    if (decodedData.Api != "v1")
                    {
                        Console.WriteLine("API version not recognized. Skipping");
                        return;
                    }
                    ClientData.Data.Enqueue(Encoding.ASCII.GetBytes(decodedData.Data));
                }
                catch (Exception)
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
        public override void ServeCallback(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Listener started");
            Connection.BeginReceive(ReceiveCallback, new ConnectionListenerContext
            {
                client = Connection,
                peer = Endpoint,
                sender = Channel,
            });
            Serving = true;
            handle.Done();
        }

        [Callback(methodName: Methods.Server.Shutdown)]
        public override void ShutdownCallback(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Shutting down listener");
            Serving = false;
            initialized = false;
            Connection.Close();
            Status = ManagedTaskStatus.Completed;
            handle.Done();
        }

        [Callback(methodName: Methods.Server.Restart)]
        public override void RestartCallback(ITaskContext context, AsyncCallHandle handle)
        {
            if (handle.Arguments.Dequeue() == true)
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

        [Callback(methodName: Methods.ClientListener.GetClientData)]
        public void GetClientDataCallback(ITaskContext context, AsyncCallHandle handle)
        {
            var doBlock = handle.Arguments.Dequeue();
            if (doBlock)
            {
                while (true)
                {
                    lock (ClientData.Mutex)
                    {
                        if (ClientData.Data.Count >= 1)
                            handle.Result = ClientData.Data.Dequeue();
                    }
                    Thread.Sleep(50);
                }
            } else
            {
                lock(ClientData.Mutex)
                {
                    handle.Result = ClientData.Data.Count >= 1 ? ClientData.Data.Dequeue() : null;
                    handle.Done();
                }
            }
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
            ClientData = new ClientListenerData();
            Channel = new Channel<byte[]>();
            Connection = new UdpClient(Endpoint);
        }

        public override void Terminate(string reason = null, params dynamic[] args)
        {
            this.Do(Methods.Server.Shutdown).Wait();
            Console.WriteLine("Server closed: '" + (reason ?? "No reason provided") + "'");
        }

    }
}

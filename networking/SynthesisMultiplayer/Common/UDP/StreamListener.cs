using MatchmakingService;
using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Threading.Runtime;
using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static SynthesisMultiplayer.Threading.Runtime.ArgumentPacker;

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

namespace SynthesisMultiplayer.Common.UDP
{
    public class StreamListener : ManagedUdpTask
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
        [SavedStateAttribute]
        ClientListenerData ClientData;
        bool disposed;
        Channel<byte[]> Channel;
        bool IsInitialized { get; set; }
        bool Serving { get; set; }

        public override bool Alive => IsInitialized;
        public override bool Initialized => IsInitialized;

        public StreamListener(IPAddress ip, int port = 33000) :
            base(ip, port)
        {
            Serving = false;
            ClientData = new ClientListenerData();
            Channel = new Channel<byte[]>();
        }
        private void ReceiveMethod(IAsyncResult result)
        {
            if (Serving)
            {
                var context = ((ConnectionListenerContext)(result.AsyncState));
                var udpClient = context.client;
                var peer = context.peer;
                var receivedData = udpClient.EndReceive(result, ref peer);
                ClientData.LastEndpoint = context.peer;
                context.sender.Send(receivedData);
                Console.WriteLine("Got Data '" + Encoding.Default.GetString(receivedData) + "'");
                context.peer = new IPEndPoint(Endpoint.Address, Endpoint.Port);
                udpClient.BeginReceive(ReceiveMethod, context);
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

        [Callback(name: Methods.Server.Serve)]
        public override void ServeMethod(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Listener started");
            Connection.BeginReceive(ReceiveMethod, new ConnectionListenerContext
            {
                client = Connection,
                peer = Endpoint,
                sender = Channel,
            });
            Serving = true;
            handle.Done();
        }

        [Callback(name: Methods.Server.Shutdown)]
        public override void ShutdownMethod(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Shutting down listener");
            Serving = false;
            IsInitialized = false;
            Connection.Close();
            Status = ManagedTaskStatus.Completed;
            handle.Done();
        }

        [Callback(name: Methods.Server.Restart)]
        public override void RestartMethod(ITaskContext context, AsyncCallHandle handle)
        {
            var doBackup = GetArgs<bool>(handle);
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

        [Callback(Methods.ClientListener.GetClientData, "doBlock")]
        [Argument("doBlock", typeof(bool), false, RuntimeArgumentAttributes.HasDefault)]
        public void GetClientDataMethod(ITaskContext context, AsyncCallHandle handle)
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
            IsInitialized = true;
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

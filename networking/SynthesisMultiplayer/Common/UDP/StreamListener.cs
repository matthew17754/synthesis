using MatchmakingService;
using Multiplayer.Attribute;
using Multiplayer.IO;
using Multiplayer.Actor;
using Multiplayer.Actor.Runtime;
using Multiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static Multiplayer.Actor.Runtime.ArgumentUnpacker;

namespace Multiplayer.Common
{
    public partial class Methods
    {
        public class StreamListener
        {
            public const string GetStreamData = "GET_STREAM_DATA";
        }
    }
}

namespace Multiplayer.Common.UDP
{
    public class StreamListener : ManagedUdpTask
    {
        protected class StreamListenerContext : TaskContext
        {
            public UdpClient client;
            public IPEndPoint peer;

            public Channel<byte[]> sender;
        }
        private class StreamListenerData
        {
            public StreamListenerData()
            {
                Data = new Queue<byte[]>();
                Mutex = new Mutex();
            }
            public Mutex Mutex;
            public Queue<byte[]> Data;
            public IPEndPoint LastEndpoint;
        }
        [SavedState]
        StreamListenerData StreamData;
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
            StreamData = new StreamListenerData();
            Channel = new Channel<byte[]>();
        }
        private void ReceiveMethod(IAsyncResult result)
        {
            if (Serving)
            {
                var context = ((StreamListenerContext)(result.AsyncState));
                var udpClient = context.client;
                var peer = context.peer;
                var receivedData = udpClient.EndReceive(result, ref peer);
                StreamData.LastEndpoint = context.peer;
                context.sender.Send(receivedData);
                Debug.Log("Got Data '" + Encoding.Default.GetString(receivedData) + "'");
                context.peer = new IPEndPoint(Endpoint.Address, Endpoint.Port);
                udpClient.BeginReceive(ReceiveMethod, context);
            }
        }
        public IPEndPoint PeekClientData(Guid id) =>
            this.Call(Methods.StreamListener.GetStreamData, false).Result;
        public IPEndPoint GetClientData(Guid id) =>
            this.Call(Methods.StreamListener.GetStreamData, true).Result;

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
                        Warning.Log("API version not recognized. Skipping");
                        return;
                    }
                    StreamData.Data.Enqueue(Encoding.ASCII.GetBytes(decodedData.Data));
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
            Info.Log($"Listener started on {Endpoint.ToString()}");
            Connection.BeginReceive(ReceiveMethod, new StreamListenerContext
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
            Serving = false;
            IsInitialized = false;
            Connection.Close();
            Status = ManagedTaskStatus.Completed;
            handle.Done();
        }

        [Callback(name: Methods.Server.Restart)]
        public override void RestartCallback(ITaskContext context, ActorCallbackHandle handle)
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

        [Callback(Methods.StreamListener.GetStreamData, "doBlock")]
        [Argument("doBlock", typeof(bool), false, ActorCallbackArgumentAttributes.HasDefault)]
        public void GetClientDataMethod(ITaskContext context, ActorCallbackHandle handle)
        {
            var doBlock = handle.Arguments.Dequeue();
            if (doBlock)
            {
                while (true)
                {
                    lock (StreamData.Mutex)
                    {
                        if (StreamData.Data.Count >= 1)
                            handle.Result = StreamData.Data.Dequeue();
                    }
                    Thread.Sleep(50);
                }
            } else
            {
                lock(StreamData.Mutex)
                {
                    handle.Result = StreamData.Data.Count >= 1 ? StreamData.Data.Dequeue() : null;
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
            StreamData = new StreamListenerData();
            Channel = new Channel<byte[]>();
            Connection = new UdpClient(Endpoint);
        }

        public override void Terminate(string reason = null, params dynamic[] args)
        {
            this.Shutdown();
            Info.Log("Server closed: '" + (reason ?? "No reason provided") + "'");
        }

    }
}

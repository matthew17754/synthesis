using Google.Protobuf;
using MatchmakingService;
using Multiplayer.Actor;
using Multiplayer.Actor.Runtime;
using Multiplayer.Attribute;
using Multiplayer.Common;
using Multiplayer.Common.UDP;
using Multiplayer.IO;
using Multiplayer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using static Multiplayer.Actor.ActorHelper;
using static Multiplayer.Actor.Runtime.ArgumentUnpacker;

namespace Multiplayer.Common
{
    public partial class Methods
    {
        public class FanoutService
        {
            public const string AddConnection = "ADD_CONNECTION";
        }
    }
}

namespace Multiplayer.Service
{
    public class FanoutService : IActor
    {
        bool IsInitialized { get; set; }
        Guid TaskId { get; set; }
        public bool Alive => IsInitialized;
        public bool Initialized => IsInitialized;

        List<(Guid Listener, Guid Sender)> Connections;

        public FanoutService(int listenerPort)
        {
            Connections = new List<(Guid, Guid)>();
        }

        public Guid Id => TaskId;

        public ManagedTaskStatus Status { get; protected set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Initialize(Guid id)
        {
            TaskId = id;
            Status = ManagedTaskStatus.Running;
            IsInitialized = true;
        }

        public void Loop()
        {
            foreach (var (listener, sender) in Connections)
            {
                var newData = Call(listener, Methods.StreamListener.GetStreamData, false);
                if (newData != null)
                {
                    var message = new ServerDataFrame
                    {
                        Api = "v1",
                        Data = Encoding.ASCII.GetString(newData),
                        MessageType = ServerDataFrame.Types.MessageType.Data,
                        ServerSendTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now),
                    };
                    var outputStream = new MemoryStream();
                    message.WriteTo(outputStream);
                    outputStream.Position = 0;
                    var outputData = new StreamReader(outputStream).ReadToEnd();
                    ((StreamSender)GetTask(sender)).Send(outputData);
                }
            }
        }

        public void AddConnection(IPAddress ip, int local, int remote) =>
            this.Call(Methods.FanoutService.AddConnection, ip, local, remote);

        [Callback(Methods.FanoutService.AddConnection, "ip", "localPort", "remotePort")]
        [Argument("ip", typeof(IPAddress))]
        [Argument("localPort", typeof(int), 0, ActorCallbackArgumentAttributes.HasDefault)]
        [Argument("remotePort", typeof(int), 0, ActorCallbackArgumentAttributes.HasDefault)]
        public void AddConnection(ITaskContext context, ActorCallbackHandle handle)
        {
            var (ip, localPort, remotePort) = GetArgs<IPAddress, int, int>(handle);
            var newListener = Start(new StreamListener(ip, localPort));
            var newSender = Start(new StreamSender(ip, remotePort));
            while (!GetTask(newSender).Initialized) { }
            while (!GetTask(newListener).Initialized) { }
            ((IServer)GetTask(newSender)).Serve();
            ((IServer)GetTask(newListener)).Serve();
            Connections.Add((newListener, newSender));
            Info.Log("New Sender '" + newSender.ToString() + "' added");
            Info.Log("New Listener '" + newListener.ToString() + "' added");
            handle.Done();
        }

        public void Terminate(string reason = null, params dynamic[] args)
        {
            IsInitialized = false;
            foreach (var (listener, sender) in Connections)
            {
                Info.Log("Fanout service shutting down");
                GetTask(listener).Terminate(reason, true);
                GetTask(sender).Terminate(reason, true);
            }
        }
   }
}

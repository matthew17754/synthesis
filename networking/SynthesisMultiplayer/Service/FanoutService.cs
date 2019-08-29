using Google.Protobuf;
using MatchmakingService;
using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Common.UDP;
using SynthesisMultiplayer.IO;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Threading.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using static SynthesisMultiplayer.Threading.ManagedTaskHelper;
using static SynthesisMultiplayer.Threading.Runtime.ArgumentPacker;
namespace SynthesisMultiplayer.Common
{
    public partial class Methods
    {
        public class FanoutService
        {
            public const string AddConnection = "ADD_CONNECTION";
        }
    }
}

namespace SynthesisMultiplayer.Service
{
    public class FanoutService : IManagedTask
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
                var newData = Call(listener, Methods.ClientListener.GetClientData, false).Result;
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

        public void AddConnection(IPAddress ip, int port) =>
            this.Do(Methods.FanoutService.AddConnection, port, ip).Wait();

        [Callback(Methods.FanoutService.AddConnection, "ip", "port")]
        [Argument("ip", typeof(IPAddress))]
        [Argument("port", typeof(int), 33000, RuntimeArgumentAttributes.HasDefault)]
        public void AddConnection(ITaskContext context, AsyncCallHandle handle)
        {
            var (ip, port) = GetArgs<IPAddress, int>(handle);
            var newListener = Start(new StreamListener(ip, port+1));
            var newSender = Start(new StreamSender(ip, port));
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
            foreach (var (listener, sender) in Connections)
            {
                Info.Log("Fanout service shutting down");
                GetTask(listener).Terminate(reason, true);
                GetTask(sender).Terminate(reason, true);
            }
        }
   }
}

using Google.Protobuf;
using MatchmakingService;
using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using static SynthesisMultiplayer.Threading.ManagedTaskHelper;

namespace SynthesisMultiplayer.Common
{
    public partial class Methods
    {
        public class FanoutService
        {
            public const string AddListener = "ADD_LISTENER";
        }
    }
}

namespace SynthesisMultiplayer.Service
{
    public class FanoutService : IManagedTask, ISupervisor
    {
        bool IsInitialized { get; set; }
        Guid TaskId { get; set; }
        public bool Alive => IsInitialized;
        public bool Initialized => IsInitialized;

        Guid ClientListener, ConnectionListener;
        List<Guid> Senders;

        public FanoutService(int listenerPort, Guid connectionListenerPid)
        {
            Senders = new List<Guid>();
            ClientListener = Start(new FanoutListener(listenerPort));
            ConnectionListener = connectionListenerPid;
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
            var newData = Call(ClientListener, Methods.ClientListener.GetClientData, false).Result;
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
                foreach (var sender in Senders)
                {
                    ((FanoutSender)GetTask(sender)).Send(outputData);
                }
            }
        }

        public void AddListener(IPAddress ip, int port) =>
            this.Do(Methods.FanoutService.AddListener, port, ip).Wait();

        [Callback(methodName: Methods.FanoutService.AddListener)]
        public void AddListenerCallback(ITaskContext context, AsyncCallHandle handle)
        {
            var ip = handle.Arguments.Dequeue();
            int port = 33000;
            try
            {
                port = handle.Arguments.Dequeue();
            }
            catch (Exception)
            {
            }
            var newSender = Start(new FanoutSender(ip, port));
            while (!GetTask(newSender).Initialized) { }
            ((IServer)GetTask(newSender)).Serve();
            Senders.Add(newSender);
            Console.WriteLine("New Sender '" + newSender.ToString() + "' added");
            handle.Done();
        }

        public void Terminate(string reason = null, params dynamic[] args)
        {
            GetTask(ClientListener).Terminate();
            foreach (var sender in Senders)
            {
                GetTask(sender).Terminate();
            }
        }

        public Guid StartChild(IManagedTask task, string name = null)
        {
            var pid = Start(task, name);
            Senders.Add(pid);
            return pid;
        }

        public void TerminateChild(Guid childId)
        {
            if (!Senders.Contains(childId) || childId != ClientListener)
                throw new Exception("Cannot terminate a processes which is unowned");
            ManagedTaskHelper.Terminate(childId);
        }

        public void TerminateChild(string childName)
        {
            throw new NotImplementedException();
        }

        public int CountChildren()
        {
            return Senders.Count + 1; // +1 for ClientListener
        }

        public List<(Guid taskId, Type type)> Children() =>
            Senders.Select(s =>
            {
                return (s, typeof(FanoutSender));
            }).Concat(new[] { (ClientListener, typeof(FanoutListener)) }).ToList();
    }
}

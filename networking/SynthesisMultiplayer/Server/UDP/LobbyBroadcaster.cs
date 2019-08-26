using MatchmakingService;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Threading.Execution;
using System;
using System.Net;
using System.Threading;
using Google.Protobuf;
using System.IO;
using System.Net.Sockets;
using SynthesisMultiplayer.Attribute;
using System.Text;
using static MatchmakingService.SessionBroadcastMessage.Types;
using System.Collections.Generic;
using System.Reflection;

namespace SynthesisMultiplayer.Common
{
    public partial class Methods
    {
        public class LobbyBroadcaster
        {
            public const string SetLobbyAttribute = "SET_LOBBY_ATTRIBUTE";
        }
    }
}

namespace SynthesisMultiplayer.Server.UDP
{
    public class LobbyBroadcaster : ManagedUdpTask
    {
        const int sendCallbackTimeout = 10000;
        [SavedState]
        Guid LobbyId { get; set; }
        [SavedState]
        string Name { get; set; }
        [SavedState]
        int Capacity { get; set; }
        [SavedState]
        string Version { get; set; }
        [SavedState]
        List<string> Tags { get; set; }
        SessionStatus SessionStatus;
        SessionBroadcastMessage message;
        SessionBroadcastMessage Message
        {
            get
            {
                if (RegenerateMessage)
                {
                    message.Api = "v1";
                    message.LobbyId = LobbyId.ToString();
                    message.LobbyName = Name;
                    message.Version = Version;
                    message.Capacity = Capacity;
                    message.Tags.Clear();
                }
                return message;
            }
            set
            {
                message = value;
            }
        }
        EventWaitHandle eventWaitHandle;
        bool RegenerateMessage { get; set; }
        bool Serving { get; set; }
        bool IsInitialized { get; set; }
        public override bool Alive => IsInitialized;

        public override bool Initialized => IsInitialized;

        public LobbyBroadcaster(int port = 33001,
            string lobbyName = "",
            int capacity = 6,
            string version = "4.3.0.0",
            SessionStatus status = SessionStatus.NotServing,
            params string[] tags) :
            base(IPAddress.Parse("255.255.255.255"), port)
        {
            LobbyId = Guid.NewGuid();
            Name = lobbyName;
            Capacity = capacity;
            Version = version;
            SessionStatus = status;
            message = new SessionBroadcastMessage
            {
                Api = "v1",
                LobbyId = LobbyId.ToString(),
                LobbyName = lobbyName,
                Capacity = capacity,
                Version = version,
                Status = SessionStatus,
            };
            foreach (var tag in tags)
            {
                Message.Tags.Add(tag);
            }
        }

        public override void Initialize(Guid taskId)
        {
            Id = taskId;
            Connection = new UdpClient();
            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            IsInitialized = true;
        }

        private void UDPSendCallback(IAsyncResult res)
        {
            if (Serving)
            {
                var bytesSent = Connection.EndSend(res);
                eventWaitHandle.WaitOne(sendCallbackTimeout);
                var outputStream = new MemoryStream();
                Message.WriteTo(outputStream);
                outputStream.Position = 0;
                var outputData = new StreamReader(outputStream).ReadToEnd();
                Connection.BeginSend(Encoding.ASCII.GetBytes(outputData),
                    outputData.Length, Endpoint.Address.ToString(),
                    Endpoint.Port, UDPSendCallback, null);
            }
        }

        [Callback(methodName: Methods.Server.Serve)]
        public override void ServeCallback(ITaskContext context, AsyncCallHandle handle)
        {
            Serving = true;
            SessionStatus = SessionStatus.Serving;
            message.Status = SessionStatus;
            Console.WriteLine("Broadcaster started");
            var outputStream = new MemoryStream();
            Message.WriteTo(outputStream);
            outputStream.Position = 0;
            var outputData = new StreamReader(outputStream).ReadToEnd();
            Connection.BeginSend(Encoding.ASCII.GetBytes(outputData),
                outputData.Length, Endpoint.Address.ToString(),
                Endpoint.Port, UDPSendCallback, null);
            outputStream.Dispose();
            handle.Done();

        }

        [Callback(methodName: Methods.Server.Shutdown)]
        public override void ShutdownCallback(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Shutting down broadcaster");
            Serving = false;
            IsInitialized = false;
            Connection.Close();
            Status = ManagedTaskStatus.Completed;
            handle.Done();
        }

        [Callback(methodName: Methods.Server.Restart)]
        public override void RestartCallback(ITaskContext context, AsyncCallHandle handle)
        {
            Terminate();
            Initialize(Id);
            handle.Done();
        }

        public override void Terminate(string reason = null, params dynamic[] args)
        {
            this.Shutdown();
            Console.WriteLine("Server closed: '" + (reason ?? "No reason provided") + "'");
        }

        [Callback(methodName: Methods.LobbyBroadcaster.SetLobbyAttribute)]
        public void SetLobbyAttributeCallback(ITaskContext context, AsyncCallHandle handle)
        {
            string attributeName = "";
            dynamic value = null;
            try
            {
                attributeName = handle.Arguments.Dequeue();
                value = handle.Arguments.Dequeue();
            }
            catch (Exception)
            {
                Console.WriteLine("Not enough arguments provided");
            }
            GetType().GetProperty(attributeName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, value);
            RegenerateMessage = true;
            handle.Done();
        }

        public void SetLobbyName(string lobbyName) =>
            this.Call(Methods.LobbyBroadcaster.SetLobbyAttribute, "Name", lobbyName).Wait();
        public void SetCapacity(int capacity) =>
            this.Call(Methods.LobbyBroadcaster.SetLobbyAttribute, "Capacity", capacity).Wait();
        public void SetVersion(string version) =>
            this.Call(Methods.LobbyBroadcaster.SetLobbyAttribute, "Version", version).Wait();
        public void SetStatus(SessionStatus status) =>
            this.Call(Methods.LobbyBroadcaster.SetLobbyAttribute, "SessoinStatus", status).Wait();

        public override void Loop()
        {
            if (Serving)
            {
                Thread.Sleep(100);
                eventWaitHandle.Set();
            }
        }
    }
}

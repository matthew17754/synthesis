using MatchmakingService;
using Multiplayer.Actor;
using Multiplayer.Actor.Runtime;
using Multiplayer.Attribute;
using Multiplayer.Common;
using Multiplayer.IO;
using Multiplayer.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static Multiplayer.Actor.Runtime.ArgumentUnpacker;

namespace Multiplayer.Common
{
    public partial class Methods
    {
        public class LobbyBroadcastListener
        {
            public const string GetLobbyList = "GET_LOBBY_LIST";
        }
    }
}

namespace Multiplayer.Client.UDP
{
    public class LobbyBroadcastListener : ManagedUdpTask
    {
        protected class ConnectionListenerContext : TaskContext
        {
            public UdpClient client;
            public IPEndPoint peer;

            public Channel<(IPAddress Peer, byte[] Data)> sender;
        }
        private class LobbyConnectionData
        {
            public LobbyConnectionData()
            {
                Mutex = new Mutex();
                Lobbies = new List<SessionBroadcastMessage>();
                LobbyConnectionInfo = new Dictionary<Guid, IPAddress>();
            }
            public Mutex Mutex;
            public List<SessionBroadcastMessage> Lobbies;
            public Dictionary<Guid, IPAddress> LobbyConnectionInfo;
            public IPEndPoint LastEndpoint;
            public bool HasLobby(string lobbyId)
            {
                foreach(var lobby in Lobbies)
                {
                    if (lobby.LobbyId == lobbyId)
                        return true;
                }
                return false;
            }
        }
        [SavedStateAttribute]
        LobbyConnectionData LobbyData;
        bool disposed;
        Channel<(IPAddress Peer, byte[] Data)> Channel;
        bool IsInitialized { get; set; }
        bool Serving { get; set; }

        public override bool Alive => IsInitialized;
        public override bool Initialized => IsInitialized;

        public LobbyBroadcastListener(int port = 33001) :
            base(IPAddress.Any, port)
        {
            Serving = false;
        }
        private void ReceiveMethod(IAsyncResult result)
        {
            if (Serving)
            {
                var context = ((ConnectionListenerContext)(result.AsyncState));
                var udpClient = context.client;
                var peer = context.peer;
                var receivedData = udpClient.EndReceive(result, ref peer);
                LobbyData.LastEndpoint = peer;
                context.sender.Send((peer.Address, receivedData));
                context.peer = new IPEndPoint(IPAddress.Any, Endpoint.Port);
                udpClient.BeginReceive(ReceiveMethod, context);
            }
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
                    var (peer, data) = newData.Get();
                    SessionBroadcastMessage decodedData = SessionBroadcastMessage.Parser.ParseFrom(data);
                    if (!LobbyData.HasLobby(decodedData.LobbyId))
                    {
                        Info.Log(decodedData.ToString());
                        if (decodedData.Api != "v1")
                        {
                            Warning.Log("API version not recognized. Skipping");
                            return;
                        }
                        Info.Log($"New lobby found: '{peer.ToString()}'. Lobby Id: '{decodedData.LobbyId}'");
                        LobbyData.Lobbies.Add(decodedData);
                        LobbyData.LobbyConnectionInfo[new Guid(decodedData.LobbyId)] = peer;
                    }
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
        public override void ShutdownCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            Info.Log("Shutting down listener");
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
            if (doBackup)
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

        [Callback(name: Methods.LobbyBroadcastListener.GetLobbyList)]
        public void GetLobbyListMethod(ITaskContext context, ActorCallbackHandle handle)
        {
            lock (LobbyData.Mutex)
                handle.Result = LobbyData.Lobbies;
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
            LobbyData = new LobbyConnectionData();
            Channel = new Channel<(IPAddress, byte[])>();
            Connection = new UdpClient(Endpoint);
        }

        public override void Terminate(string reason = null, params dynamic[] args)
        {
            this.Call(Methods.Server.Shutdown);
            Info.Log("Server closed: '" + (reason ?? "No reason provided") + "'");
        }
    }
}

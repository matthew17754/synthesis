﻿using MatchmakingService;
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
using static MatchmakingService.SessionBroadcastMessage.Types;

namespace SynthesisMultiplayer.Common
{
    public partial class Methods
    {
        public class LobbyBroadcastListener
        {
            public const string GetLobbyList = "GET_LOBBY_LIST";
        }
    }
}


namespace SynthesisMultiplayer.Client.UDP
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
        [SavedState]
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
        private void ReceiveCallback(IAsyncResult result)
        {
            if (Serving)
            {
                var context = ((ConnectionListenerContext)(result.AsyncState));
                var udpClient = context.client;
                var peer = context.peer;
                var receivedData = udpClient.EndReceive(result, ref context.peer);
                LobbyData.LastEndpoint = context.peer;
                context.sender.Send((peer.Address, receivedData));
                context.peer = new IPEndPoint(IPAddress.Any, Endpoint.Port);
                udpClient.BeginReceive(ReceiveCallback, context);
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

                        if (decodedData.Api != "v1")
                        {
                            Console.WriteLine("API version not recognized. Skipping");
                            return;
                        }
                        Console.WriteLine("New lobby found: '{0}'. Lobby Id: '{1}'", peer.ToString(), decodedData.LobbyId);
                        LobbyData.Lobbies.Add(decodedData);
                        LobbyData.LobbyConnectionInfo[new Guid(decodedData.LobbyId)] = peer;
                    }
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
            IsInitialized = false;
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

        [Callback(methodName: Methods.LobbyBroadcastListener.GetLobbyList)]
        public void GetLobbyListCallback(ITaskContext context, AsyncCallHandle handle)
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
            this.Do(Methods.Server.Shutdown).Wait();
            Console.WriteLine("Server closed: '" + (reason ?? "No reason provided") + "'");
        }
    }
}

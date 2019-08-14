using System;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Common;
using System.Threading;
using SynthesisMultiplayer.Service;
using System.Net;
using SynthesisMultiplayer.Client.UDP;
using MatchmakingService;
using System.Collections.Generic;
using static SynthesisMultiplayer.Threading.ManagedTaskHelper;

namespace MultiplayerServer
{
    public class Application
    {
        public static void Main(string[] args)
        {
            Start(new ConnectionListener(33003), "listener");
            Start(new LobbyBroadcaster(lobbyName: "Test Lobby"), "broadcaster");
            Start(new LobbyBroadcastListener(), "broadcast_listener");
            var listener = (ConnectionListener) GetTask("listener");
            var broadcast  = (LobbyBroadcaster) GetTask("broadcaster");
            var broadcastListener = (LobbyBroadcastListener)GetTask("broadcast_listener");
            Start(new FanoutService(50054, listener.Id), "fanout");
            listener.Serve();
            broadcast.Serve();
            broadcastListener.Serve();
            Thread.Sleep(500);
            var Lobbies = (List<SessionBroadcastMessage>) Call(broadcastListener.Id, Methods.LobbyBroadcastListener.GetLobbyList).Result;
            foreach(var lobby in Lobbies)
            {
                Console.WriteLine("Lobby: {0} LobbyName: {1}", lobby.LobbyId, lobby.LobbyName);
            }
            var fanout = (FanoutService)GetTask("fanout");
            fanout.AddListener(IPAddress.Parse("127.0.0.1"), 5000);
            while(Console.ReadKey(true).Key != ConsoleKey.Escape) { }
            broadcast.Terminate();
            listener.Terminate();
            broadcastListener.Terminate();

            Console.WriteLine("Server Closing. Please wait...");
            int counter = 0;
            while (broadcast.Status != ManagedTaskStatus.Completed)
            {
                if(counter % 50 == 0)
                {
                    Console.Write(".");
                }
                ++counter;
            }
        }
    }
}

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
using SynthesisMultiplayer.Server.gRPC;
using SynthesisMultiplayer.Client.gRPC;

namespace MultiplayerServer
{
    public class Application
    {
        public static void Main(string[] args)
        {
            var lobby = Start(new LobbyService());
            Start(new LobbyBroadcastListener(), "broadcast_listener");
            Start(new LobbyClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 33005)), "lobby_client");
            var LobbyService = (LobbyService)GetTask(lobby);
            while(!LobbyService.Initialized) { }
            var lobbyClient = (LobbyClient)GetTask("lobby_client");
            LobbyService.Serve();
            lobbyClient.JoinLobby();
            Thread.Sleep(500);
            while(Console.ReadKey(true).Key != ConsoleKey.Escape) { }
            LobbyService.Terminate();

            Console.WriteLine("Server Closing. Please wait...");
            int counter = 0;
            while (LobbyService.Status != ManagedTaskStatus.Completed)
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

using System;
using SynthesisMultiplayer.Common;
using System.Threading;
using SynthesisMultiplayer.Service;
using SynthesisMultiplayer.Threading;
using static SynthesisMultiplayer.Threading.ManagedTaskHelper;
namespace MultiplayerServer
{
    public class Application
    {
        public static void Main(string[] args)
        {
            var lobby = Start(new LobbyService());
            var client = Start(new LobbyClientService());
            var LobbyService = (LobbyService)GetTask(lobby);
            while(!LobbyService.Initialized) { }
            LobbyService.Serve();
            var lobbyClient = (LobbyClientService)GetTask(client);
            lobbyClient.JoinLobby("127.0.0.1:33005");
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

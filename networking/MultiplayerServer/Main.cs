using System;
using Multiplayer.Common;
using System.Threading;
using Multiplayer.Service;
using Multiplayer.Actor;
using Multiplayer.IO;
using static Multiplayer.Actor.ActorHelper;
using System.IO;
using System.Text;
using Multiplayer.Common.UDP;
using System.Net;

namespace MultiplayerServer
{
    public class Application
    {
        public static void Main(string[] args)
        {
            var file = new StreamWriter(new FileStream(@"C:\Users\t_burrn\test.txt", FileMode.Append));
            Logger.RegisterLogger(Logger.LogLevel.Info,
                new MultiWriter(true,
                new LogWriter((s) =>
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[ ] " + DateTime.Now + ": " + s);
                    Console.ForegroundColor = ConsoleColor.White;
                })
                , file));
            Logger.RegisterLogger(Logger.LogLevel.Debug,
                new MultiWriter(true,
                new LogWriter((s) =>
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("[ ] " + DateTime.Now + ": " + s);
                })
                , file));
            Logger.RegisterLogger(Logger.LogLevel.Warning,
                new MultiWriter(true,
                new LogWriter((s) =>
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("[*] " + DateTime.Now + ": " + s);
                    Console.ForegroundColor = ConsoleColor.White;
                })
                , file));
            Logger.RegisterLogger(Logger.LogLevel.Error,
                new MultiWriter(true,
                new LogWriter((s) =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[!] " + DateTime.Now + ": " + s);
                    Console.ForegroundColor = ConsoleColor.White;
                })
                , file));
            var lobby = Start(new LobbyService());
            var client = Start(new LobbyClientService());
            var LobbyService = (LobbyService)GetTask(lobby);
            while (!LobbyService.Initialized)
            {
            }
            LobbyService.Serve();
            var lobbyClient = (LobbyClientService)GetTask(client);
            lobbyClient.Connect("127.0.0.1:33005");
            lobbyClient.Call("send", "test");
            lobbyClient.Call("send", "test");
            lobbyClient.Call("send", "test");
            lobbyClient.Call("send", "test");
            lobbyClient.Call("send", "test");
            var sender = Start(new StreamSender(IPAddress.Parse("127.0.0.1"), 51200));
            var senderTask = ((StreamSender)GetTask(sender));
            while (!senderTask.Initialized) { }
            senderTask.Serve();
            Thread.Sleep(500);
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
            LobbyService.Terminate();
            senderTask.Terminate();
            lobbyClient.Terminate();
            Info.Log("Server Closing. Please wait...");
            int counter = 0;
            while (LobbyService.Status != ManagedTaskStatus.Completed)
            {
                if (counter % 50 == 0)
                {
                    Console.Write(".");
                }
                ++counter;
            }
            CleanupTasks();
        }
    }
}

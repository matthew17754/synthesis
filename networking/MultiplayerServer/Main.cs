using Multiplayer.Actor;
using Multiplayer.IO;
using Multiplayer.Server;
using Multiplayer.Service;
using System;
using System.IO;
using System.Threading;
using static Multiplayer.Actor.ActorHelper;

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
            var lobbyService = (LobbyService)GetTask(lobby);
            lobbyService.Serve();
            Thread.Sleep(500);
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
            lobbyService.Terminate();
            Info.Log("Server Closing. Please wait...");
            CleanupTasks();
        }
    }
}

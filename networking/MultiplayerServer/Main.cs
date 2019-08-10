using System;
using SynthesisMultiplayer.Server.Methods;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Threading.Methods;
using SynthesisMultiplayer.Util;

namespace MultiplayerServer
{
    public class Application
    {
        public static void Main(string[] args)
        {
            var test1 = ManagedTaskHelper.Start(new ConnectionListener(), "listener");
            var test2 = ManagedTaskHelper.Start(new LobbyHostBroadcaster(), "broadcaster");
            ManagedTaskHelper.GetTask(test1).Call(Server.Serve);
            ManagedTaskHelper.GetTask(test2).Call(Server.Serve);
            while (Console.ReadKey(true).Key != ConsoleKey.Enter)
            { }

            ManagedTaskHelper.GetTask(test1).Do(Default.Task.Exit);
            Console.WriteLine("Server Closing. Please wait...");
            int counter = 0;
            while (ManagedTaskHelper.GetTask(test1).GetStatus() != ManagedTaskStatus.Completed)
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

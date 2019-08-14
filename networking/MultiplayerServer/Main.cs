using System;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Common;
using static SynthesisMultiplayer.Threading.ManagedTaskHelper;
using System.Threading;
using SynthesisMultiplayer.Service;
using System.Net;

namespace MultiplayerServer
{
    public class Application
    {
        public static void Main(string[] args)
        {
            Start(new ConnectionListener(), "listener");
            Start(new LobbyHostBroadcaster(), "broadcaster");
            var listener = (ConnectionListener) GetTask("listener");
            var broadcast  = (LobbyHostBroadcaster) GetTask("broadcaster");
            Start(new FanoutService(50054, listener.Id), "fanout");
            listener.Serve();
            broadcast.Serve();
            var fanout = (FanoutService)GetTask("fanout");
            fanout.AddListener(IPAddress.Parse("127.0.0.1"), 5000);
            while(Console.ReadKey(true).Key != ConsoleKey.Escape) { }
            Thread.Sleep(500);
            broadcast.Terminate();
            listener.Terminate();

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

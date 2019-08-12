using System;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Common;
using static SynthesisMultiplayer.Threading.ManagedTaskHelper;
namespace MultiplayerServer
{
    public class Application
    {
        public static void Main(string[] args)
        {
            Start(new ConnectionListener(), "listener");
            Start(new LobbyHostBroadcaster(), "broadcaster");
            ConnectionListener listener = (ConnectionListener)GetTask("listener");
            LobbyHostBroadcaster broadcast  = (LobbyHostBroadcaster)GetTask("broadcaster");
            listener.Serve();
            broadcast.Serve();
            while (Console.ReadKey(true).Key != ConsoleKey.Enter)
            { }
            listener.Restart(false);
            while (Console.ReadKey(true).Key != ConsoleKey.Enter)
            { }
            listener.Terminate();

            Console.WriteLine("Server Closing. Please wait...");
            int counter = 0;
            while (listener.Status != ManagedTaskStatus.Completed)
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

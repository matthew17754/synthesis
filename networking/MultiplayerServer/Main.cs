using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerServer;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Util;
using SynthesisMultiplayer.Threading.Message;

namespace MultiplayerServer
{
    public class main
    {
        public static void Main(string[] args)
        {
            var (send, recv) = Channel<(string, AsyncCallHandle?)>.CreateMPSCChannel();
            var test = new ListenerServer(send, recv);
            ManagedTaskHelper.Run(test, new TaskContext());
            int iterator = 0;
            while (true)
            {
                if (iterator >= 1000)
                {
                    test.Call(Default.Task.Exit);
                }
                if (test.GetState() != null && test.GetState() == Default.State.GracefulExit) {
                    Console.WriteLine("Exited");
                    while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
                    break;
                }
                iterator++;
            }
        }
    }
}

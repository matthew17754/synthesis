using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerServer;
using SynthesisMultiplayer.Server.UDP;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Util;

namespace MultiplayerServer
{
    public class main
    {
        public static void Main(string[] args)
        {
            var (send, _) = Channel<IMessage>.CreateMPSCChannel();
            var (_, recv) = Channel<IMessage>.CreateMPSCChannel();
            var test = new ListenerServer(send, recv);
            ManagedTaskHelper.Run(test, new TaskContextBase());
            while (true)
            {

            }
        }
    }
}

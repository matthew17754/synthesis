using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Util;
using MatchmakingService;
using System.Text;

namespace SynthesisMultiplayer.Server.UDP
{
    public class ListenerServer : ManagedUDPTask
    {
        protected class ListenerContext : TaskContextBase
        {
            public UdpClient client;
            public IPEndPoint peer;

            public Channel<byte[]> sender;
        }
        private class ListenerServerData
        {
            public ListenerServerData()
            {
                Mutex = new Mutex();
                ConnectionInfo = new Dictionary<Guid, IPEndPoint>();
            }
            public Mutex Mutex;
            public Dictionary<Guid, IPEndPoint> ConnectionInfo;
            public IPEndPoint LastEndpoint;
        }

        ListenerServerData ServerData;
        Channel<byte[]> SendChannel, ReceiveChannel;
        public ListenerServer(Channel<IMessage> statusChannel, Channel<IMessage> messageChannel, int port = 33000) :
            base(statusChannel, messageChannel, IPAddress.Any, port) { }
        private void receiveCallback(IAsyncResult result)
        {
            var context = ((ListenerContext)(result.AsyncState));
            var udpClient = context.client;
            var peer = context.peer;
            var receivedData = udpClient.EndReceive(result, ref context.peer);
            ServerData.LastEndpoint = context.peer;
            context.sender.Send(receivedData);
            Console.WriteLine("Got Data '" + Encoding.Default.GetString(receivedData) + "'");
            context.peer = new IPEndPoint(IPAddress.Any, Endpoint.Port);
            udpClient.BeginReceive(receiveCallback, context);
        }

        private IPEndPoint GetConnectionInfo(Guid id)
        {
            lock (ServerData.Mutex)
            {
                if (ServerData.ConnectionInfo.ContainsKey(id))
                {
                    return ServerData.ConnectionInfo[id];
                }
                else
                {
                    return null;
                }
            }
        }

        public override void OnStart(ref ITaskContext context)
        {
            Console.WriteLine("Server started");
            ServerData = new ListenerServerData();
            (SendChannel, ReceiveChannel) = Channel<byte[]>.CreateMPSCChannel();
            Connection = new UdpClient(Endpoint);
            Connection.BeginReceive(receiveCallback, new ListenerContext
            {
                client = Connection,
                peer = Endpoint,
                sender = SendChannel,
            });
            base.OnStart(ref context);
        }

        public override void OnResume(ref ITaskContext context)
        {
            base.OnResume(ref context);
        }

        public override void OnCycle(ref ITaskContext context)
        {
            var newData = SendChannel.TryGet();
            if (!newData.IsValid())
            {
                base.OnCycle(ref context);
                return;
            }
            try
            {
                var decodedData = UDPValidatorMessage.Parser.ParseFrom(newData);

                if (decodedData.Api != "v1")
                {
                    Console.WriteLine("API version not recognized. Skipping");
                    base.OnCycle(ref context);
                    return;
                }
                ServerData.ConnectionInfo[new Guid(decodedData.JobId)] = ServerData.LastEndpoint;
            } 
            catch(Exception e)
            {
                    Console.WriteLine("API version not recognized. Skipping");
                    base.OnCycle(ref context);
                    return;
            }
        }

        public override void OnPause(ref ITaskContext context)
        {
            base.OnPause(ref context);
        }

        public override void OnStop(ref ITaskContext context)
        {
            base.OnStop(ref context);
        }

        public override void OnExit(ref ITaskContext context)
        {
            base.OnExit(ref context);
        }
    }
}

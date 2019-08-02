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
using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Threading.Message;

namespace SynthesisMultiplayer.Server.UDP
{
    public class ListenerServer : ManagedUDPTask
    {
        protected class ListenerContext : TaskContext
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
        public ListenerServer(Channel<(IMessage, AsyncCallHandle?)> statusChannel, Channel<(IMessage, AsyncCallHandle?)> messageChannel, int port = 33000) :
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

        public override void OnStart(ITaskContext context, AsyncCallHandle? handle)
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
            base.OnStart(context, handle);
        }

        public override void OnResume(ITaskContext context, AsyncCallHandle? handle)
        {
            base.OnResume(context, handle);
        }

        public override void OnCycle(ITaskContext context, AsyncCallHandle? handle)
        {
            var newData = SendChannel.TryGet();
            if (!newData.IsValid())
            {
                base.OnCycle(context, handle);
                return;
            }
            try
            {
                var decodedData = UDPValidatorMessage.Parser.ParseFrom(newData);

                if (decodedData.Api != "v1")
                {
                    Console.WriteLine("API version not recognized. Skipping");
                    base.OnCycle(context, handle);
                    return;
                }
                ServerData.ConnectionInfo[new Guid(decodedData.JobId)] = ServerData.LastEndpoint;
            } 
            catch(Exception e)
            {
                    Console.WriteLine("API version not recognized. Skipping");
                    base.OnCycle(context, handle);
                    return;
            }
        }

        public override void OnPause(ITaskContext context, AsyncCallHandle? handle)
        {
            base.OnPause(context, handle);
        }

        public override void OnStop(ITaskContext context, AsyncCallHandle? handle)
        {
            base.OnStop(context, handle);
        }
        public override void OnExit(ITaskContext context, AsyncCallHandle? handle)
        {
            Console.WriteLine("Exiting");
            base.OnExit(context, handle);
        }
    }
}

﻿using System;
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
using SynthesisMultiplayer.Threading.Methods;

namespace SynthesisMultiplayer.Server.UDP
{
    public class ConnectionListener : ManagedUDPTask
    {
        protected class ConnectionListenerContext : TaskContext
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
        [SavedState]
        ListenerServerData ServerData;
        bool disposed;
        Channel<byte[]> Channel;
        bool started = false;
        public ConnectionListener(int port = 33000) :
            base(IPAddress.Any, port) { }
        private void ReceiveCallback(IAsyncResult result)
        {
            var context = ((ConnectionListenerContext)(result.AsyncState));
            var udpClient = context.client;
            var peer = context.peer;
            var receivedData = udpClient.EndReceive(result, ref context.peer);
            ServerData.LastEndpoint = context.peer;
            context.sender.Send(receivedData);
            Console.WriteLine("Got Data '" + Encoding.Default.GetString(receivedData) + "'");
            context.peer = new IPEndPoint(IPAddress.Any, Endpoint.Port);
            udpClient.BeginReceive(ReceiveCallback, context);
        }
        private IPEndPoint GetConnectionInfo(Guid id)
        {
            lock (ServerData.Mutex)
                return ServerData.ConnectionInfo.ContainsKey(id) ? ServerData.ConnectionInfo[id] : null;
        }
        public override void OnCycle(ITaskContext context, AsyncCallHandle handle)
        {
            if (started)
            {
                var newData = Channel.TryGet();
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
                    base.OnCycle(context, handle);
                }
                catch (Exception e)
                {
                    Console.WriteLine("API version not recognized. Skipping");
                    base.OnCycle(context, handle);
                    return;
                }
            } else
            {
                Thread.Sleep(50);
                base.OnCycle(context, handle);
            }
        }

        [Callback(methodName: Server.Methods.Server.Serve)]
        public override void Serve(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Listener started");
            ServerData = new ListenerServerData();
            Channel = new Channel<byte[]>();
            Connection = new UdpClient(Endpoint);
            Connection.BeginReceive(ReceiveCallback, new ConnectionListenerContext
            {
                client = Connection,
                peer = Endpoint,
                sender = Channel,
            });
            started = true;
        }

        [Callback(methodName: Server.Methods.Server.Shutdown)]
        public override void Shutdown(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Shutting down listener");
            Call(Default.Task.Exit);
        }

        [Callback(methodName: Server.Methods.Server.Restart)]
        public override void Restart(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Connection.Close();
                    Connection.Dispose();
                    MessageChannel.Dispose();
                }
                disposed = true;
                Dispose();
            }
        }

    }
}
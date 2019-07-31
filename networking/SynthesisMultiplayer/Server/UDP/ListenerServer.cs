using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using SynthesisMultiplayer.Threading;

namespace SynthesisMultiplayer.Server.UDP
{
    class ListenerServer
    {
        int Port;

        public ListenerServer(int port = 33000)
        {
            Port = port;
        }

        protected class ListenerContext : TaskContextBase
        {
            public UdpClient client;
            public IPEndPoint peer;
        }
        private sealed class ListenerServerData
        {
            private ListenerServerData()
            {
                mutex = new Mutex();
                connInfo = new Dictionary<Guid, Common.ConnectionInfo>();
            }
            public static ListenerServerData Instance { get => InstanceData.instance; }
            private class InstanceData
            {
                static InstanceData() { }
                internal static readonly ListenerServerData instance = new ListenerServerData();
            }
            public Mutex mutex;
            public Dictionary<Guid, Common.ConnectionInfo> connInfo;
        }
        private void receiveCallback(IAsyncResult result)
        {
            var context = ((ListenerContext)(result.AsyncState));
            var udpClient = context.client;
            var peer = context.peer;
            var data = MatchmakingService.UDPValidatorMessage.Parser.ParseFrom(
                udpClient.EndReceive(result, ref peer));
            if (data.Api != "v1")
            {
                Console.WriteLine("Error: could not understand API version '" + data.Api + "'");
                udpClient.BeginReceive(new AsyncCallback(receiveCallback), context);
            }
            SetConnectionInfo(new Guid(data.JobId), new Common.ConnectionInfo {
                    Ip = peer.Address.ToString(),
                    Port = peer.Port,
            });
            context.peer = new IPEndPoint(IPAddress.Any, Port);
            udpClient.BeginReceive(receiveCallback, context);
        }
        private Common.ConnectionInfo? GetConnectionInfo(Guid id)
        {
            lock (ListenerServerData.Instance.mutex)
            {
                if (ListenerServerData.Instance.connInfo.ContainsKey(id))
                {
                    return ListenerServerData.Instance.connInfo[id];
                }
                else
                {
                    return null;
                }
            }
        }
        private void SetConnectionInfo(Guid id, Common.ConnectionInfo connInfo)
        {
            lock(ListenerServerData.Instance.mutex)
            {
                ListenerServerData.Instance.connInfo[id] = connInfo;
            }
        }
    }
}

using MatchmakingService;
using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Common
{
    public partial class Methods
    {
        public class LobbyRegistrar
        {
            public const string Register = "REGISTER";
        }
    }
}

namespace SynthesisMultiplayer.Server.UDP
{
    public class LobbyRegistrar : ManagedUdpTask
    {
        const int sendCallbackTimeout = 10000;
        EventWaitHandle eventWaitHandle;
        bool Serving { get; set; }
        bool initialized { get; set; }
        Channel<byte[]> sendQueue;
        public override bool Alive => initialized;

        public override bool Initialized => initialized;

        public LobbyRegistrar(IPAddress ip, int port = 33001) :
            base(ip, port)
        {
            sendQueue = new Channel<byte[]>();
        }

        public override void Initialize(Guid taskId)
        {
            Id = taskId;
            Connection = new UdpClient();
            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            initialized = true;
        }

        public void Register(IPAddress lobbyEndpoint, string jobId)
        {
            this.Call(Methods.ClientSender.Send, lobbyEndpoint, jobId).Wait();
        }

        private void UDPSendCallback(IAsyncResult res)
        {
            if (Serving)
            {
                using (var message = sendQueue.TryGet())
                {
                    if (message.Valid)
                    {
                        var outputData = message.Get();
                        var bytesSent = Connection.EndSend(res);
                        eventWaitHandle.WaitOne(sendCallbackTimeout);
                        Connection.BeginSend(outputData,
                            outputData.Length, Endpoint.Address.ToString(),
                            Endpoint.Port, UDPSendCallback, null);
                    }
                }
            }
        }

        [Callback(methodName: Methods.LobbyRegistrar.Register)]
        public void RegisterCallback(ITaskContext context, AsyncCallHandle handle)
        {
            var lobbyHost = (IPEndPoint)handle.Arguments.Dequeue();
            var jobId = handle.Arguments.Dequeue();
            var message = new UDPValidatorMessage {
                Api = "v1",
                JobId = jobId
            };
            eventWaitHandle.Set();
        }

        [Callback(methodName: Methods.Server.Serve)]
        public override void ServeCallback(ITaskContext context, AsyncCallHandle handle)
        {
            Serving = true;
            Console.WriteLine("Lobby registrar started");
            Connection.BeginSend(new byte[] { },
                0, Endpoint.Address.ToString(),
                Endpoint.Port, UDPSendCallback, null);
            handle.Done();
        }

        [Callback(methodName: Methods.Server.Shutdown)]
        public override void ShutdownCallback(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Shutting down registrar");
            Serving = false;
            initialized = false;
            Connection.Close();
            Status = ManagedTaskStatus.Completed;
            handle.Done();
        }

        [Callback(methodName: Methods.Server.Restart)]
        public override void RestartCallback(ITaskContext context, AsyncCallHandle handle)
        {
            Terminate();
            Initialize(Id);
            handle.Done();
        }

        public override void Terminate(string reason = null, params dynamic[] args)
        {
            this.Shutdown();
            Console.WriteLine("Server closed: '" + (reason ?? "No reason provided") + "'");
        }

        public override void Loop()
        {
            if (Serving)
            {
                Thread.Sleep(100);
                eventWaitHandle.Set();
            }
        }

    }
}

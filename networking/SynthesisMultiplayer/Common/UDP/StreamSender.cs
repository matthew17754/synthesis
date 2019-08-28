using Google.Protobuf;
using SynthesisMultiplayer.Attribute;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Threading.Runtime;
using SynthesisMultiplayer.Util;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SynthesisMultiplayer.Common
{
    public partial class Methods
    {
        public class ClientSender
        {
            public const string Send = "SEND";
        }
    }
}

namespace SynthesisMultiplayer.Common.UDP
{
    public class StreamSender : ManagedUdpTask
    {
        const int sendMethodTimeout = 10000;
        EventWaitHandle eventWaitHandle;
        bool Serving { get; set; }
        bool initialized { get; set; }
        Channel<byte[]> sendQueue;
        public override bool Alive => initialized;

        public override bool Initialized => initialized;

        public StreamSender(IPAddress ip, int port = 33001) :
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

        public void Send(string data)
        {
            this.Call(Methods.ClientSender.Send, Encoding.ASCII.GetBytes(data)).Wait();
        }

        private void UDPSendMethod(IAsyncResult res)
        {
            if (Serving)
            {
                using (var message = sendQueue.TryGet())
                {
                    if (message.Valid)
                    {
                        var outputData = message.Get();
                        var bytesSent = Connection.EndSend(res);
                        eventWaitHandle.WaitOne(sendMethodTimeout);
                        Connection.BeginSend(outputData,
                            outputData.Length, Endpoint.Address.ToString(),
                            Endpoint.Port, UDPSendMethod, null);
                    }
                }
            }
        }

        [Callback(name: Methods.ClientSender.Send)]
        [ArgumentAttribute("sendData",typeof(byte[]))]
        public void SendMethod(ITaskContext context, AsyncCallHandle handle)
        {
            byte[] sendData = ArgumentPacker.GetArgs<byte[]>(handle);
            sendQueue.Send(sendData);
            handle.Done();
        }

        [Callback(name: Methods.Server.Serve)]
        public override void ServeMethod(ITaskContext context, AsyncCallHandle handle)
        {
            Serving = true;
            Console.WriteLine("Fanout sender started");
            Connection.BeginSend(new byte[] { },
                0, Endpoint.Address.ToString(),
                Endpoint.Port, UDPSendMethod, null);
            handle.Done();
        }

        [Callback(name: Methods.Server.Shutdown)]
        public override void ShutdownMethod(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Shutting down broadcaster");
            Serving = false;
            initialized = false;
            Connection.Close();
            Status = ManagedTaskStatus.Completed;
            handle.Done();
        }

        [Callback(name: Methods.Server.Restart)]
        public override void RestartMethod(ITaskContext context, AsyncCallHandle handle)
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

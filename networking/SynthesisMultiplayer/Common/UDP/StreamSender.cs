using Google.Protobuf;
using Multiplayer.Attribute;
using Multiplayer.Common;
using Multiplayer.IO;
using Multiplayer.Actor;
using Multiplayer.Actor.Runtime;
using Multiplayer.Util;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Multiplayer.Common
{
    public partial class Methods
    {
        public class ClientSender
        {
            public const string Send = "SEND";
        }
    }
}

namespace Multiplayer.Common.UDP
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

        [Callback(Methods.ClientSender.Send, "sendData")]
        [Argument("sendData", typeof(byte[]))]
        public void SendMethod(ITaskContext context, ActorCallbackHandle handle)
        {
            byte[] sendData = ArgumentUnpacker.GetArgs<byte[]>(handle);
            sendQueue.Send(sendData);
            handle.Done();
        }

        [Callback(name: Methods.Server.Serve)]
        public override void ServeCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            Serving = true;
            Info.Log($"Stream Sender started on {Endpoint.ToString()}");
            handle.Done();
        }

        [Callback(name: Methods.Server.Shutdown)]
        public override void ShutdownCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            Serving = false;
            initialized = false;
            Connection.Close();
            Status = ManagedTaskStatus.Completed;
            handle.Done();
        }

        [Callback(name: Methods.Server.Restart)]
        public override void RestartCallback(ITaskContext context, ActorCallbackHandle handle)
        {
            Terminate();
            Initialize(Id);
            handle.Done();
        }

        public override void Terminate(string reason = null, params dynamic[] args)
        {
            this.Shutdown();
            Info.Log("Server closed: '" + (reason ?? "No reason provided") + "'");
        }

        public override void Loop()
        {
            if (Serving)
            {
                if (sendQueue.TryPeek().Valid)
                {
                    var outputData = sendQueue.Get();
                    Connection.SendAsync(outputData,
                        outputData.Length, Endpoint);
                }
            }
        }
    }
}

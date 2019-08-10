using MatchmakingService;
using SynthesisMultiplayer.Common;
using SynthesisMultiplayer.Threading;
using SynthesisMultiplayer.Threading.Methods;
using SynthesisMultiplayer.Util;
using System;
using System.Net;
using System.Threading;
using Google.Protobuf;
using System.IO;
using System.Net.Sockets;
using SynthesisMultiplayer.Attribute;
using System.Text;

namespace SynthesisMultiplayer.Server.UDP
{
    public class LobbyHostBroadcaster : ManagedUDPTask
    {

        const int sendCallbackTimeout = 10000;
        [SavedState]
        Guid LobbyCode { get; set; }
        EventWaitHandle eventWaitHandle;
        readonly SessionBroadcastMessage message;
        bool started = false;
        public LobbyHostBroadcaster(int port = 33001) :
            base(IPAddress.Parse("255.255.255.255"), port)
        {
            LobbyCode = Guid.NewGuid();
            message = new SessionBroadcastMessage
            {
                Api = "v1",
                LobbyId = LobbyCode.ToString(),
            };
            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        private void SendCallback(IAsyncResult res)
        {
            var bytesSent = Connection.EndSend(res);
            eventWaitHandle.WaitOne(sendCallbackTimeout);
            var outputStream = new MemoryStream();
            message.WriteTo(outputStream);
            outputStream.Position = 0;
            var outputData = new StreamReader(outputStream).ReadToEnd();
            Console.WriteLine("Sending: " + outputData);
            Connection.BeginSend(Encoding.ASCII.GetBytes(outputData),
                outputData.Length, Endpoint.Address.ToString(),
                Endpoint.Port, SendCallback, null);
        }

        public override void OnCycle(ITaskContext context, AsyncCallHandle handle)
        {
            if (started)
            {
                Thread.Sleep(100);
                eventWaitHandle.Set();
                base.OnCycle(context, handle);
            }
            else
            {
                base.OnCycle(context, handle);
            }
        }

        [Callback(methodName: Server.Methods.Server.Serve)]
        public override void Serve(ITaskContext context, AsyncCallHandle handle)
        {
            Connection = new UdpClient();
            started = true;
            Console.WriteLine("Broadcaster started");
            var outputStream = new MemoryStream();
            message.WriteTo(outputStream);
            outputStream.Position = 0;
            var outputData = new StreamReader(outputStream).ReadToEnd();
            Console.WriteLine("Sending: " + outputData);
            Connection.BeginSend(Encoding.ASCII.GetBytes(outputData),
                outputData.Length, Endpoint.Address.ToString(),
                Endpoint.Port, SendCallback, null);
            outputStream.Dispose();
        }

        [Callback(methodName: Methods.Server.Shutdown)]
        public override void Shutdown(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Shutting down broadcaster");
            Call(Default.Task.Exit);
        }

        [Callback(methodName: Methods.Server.Restart)]
        public override void Restart(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }


    }

}

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
        [SavedState]
        readonly SessionBroadcastMessage message;
        EventWaitHandle eventWaitHandle;
        bool Serving { get; set; }
        bool initialized { get; set; }
        public override bool Alive => initialized;

        public override bool Initialized => initialized;

        public LobbyHostBroadcaster(int port = 33001) :
            base(IPAddress.Parse("255.255.255.255"), port)
        {
            LobbyCode = Guid.NewGuid();
            message = new SessionBroadcastMessage
            {
                Api = "v1",
                LobbyId = LobbyCode.ToString(),
            };
        }

        public override void Initialize()
        {
            Connection = new UdpClient();
            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            initialized = true;
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

        [Callback(methodName: Methods.Server.Serve)]
        public override void Serve(ITaskContext context, AsyncCallHandle handle)
        {
            Serving = true;
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
            handle.Ready = true;

        }

        [Callback(methodName: Methods.Server.Shutdown)]
        public override void Shutdown(ITaskContext context, AsyncCallHandle handle)
        {
            Console.WriteLine("Shutting down broadcaster");
            this.Call(Default.Task.Exit);
        }

        [Callback(methodName: Methods.Server.Restart)]
        public override void Restart(ITaskContext context, AsyncCallHandle handle)
        {
            throw new NotImplementedException();
        }

        public override void Terminate(string reason = null, System.Collections.Generic.Dictionary<string, dynamic> state = null)
        {
            this.Call(Methods.Server.Shutdown).Wait();
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

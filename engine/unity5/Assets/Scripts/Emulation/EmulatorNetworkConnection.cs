﻿using System;
using EmulationService;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synthesis
{
    public class EmulatorNetworkConnection : MonoBehaviour
    {
        public static EmulatorNetworkConnection Instance { get; private set; }

        public const string DEFAULT_PORT = "50051";
        public const string DEFAULT_NATIVE_PORT = "50052";
        public const string DEFAULT_JAVA_PORT = "50053";
   
        private const int LOOP_DELAY = 50; // ms
        private const int ERROR_DELAY = 100; // ms

        private bool senderConnected = false, receiverConnected = false;

        private const string API_VERSION = "v1";

        private bool isConnectionOpen = false;

        /*
        private int? TIMEOUT = null;
        private uint? RETRIES = 5;

        private Channel<IMessage> inputCommander, inputListener;
        private Channel<IMessage> outputCommander, outputListener;

        private SenderTask sender;
        private ReceiverTask receiver;

        private System.Threading.Thread senderThread, receiverThread;
        */

        public void Awake()
        {
            Instance = this;

            /*
            (inputCommander, inputListener) = Channel<IMessage>.CreateOneshot<IMessage>();
            (outputCommander, outputListener) = Channel<IMessage>.CreateOneshot<IMessage>();

            sender = new SenderTask(inputCommander, inputListener, Ip, Port, TIMEOUT, RETRIES);
            senderThread = ManagedTaskRunner.Create(sender);

            receiver = new ReceiverTask(outputCommander, outputListener, Ip, Port, TIMEOUT, RETRIES);
            receiverThread = ManagedTaskRunner.Create(receiver);
            
            senderThread.Start();
            receiverThread.Start();
            */
        }

        public void OpenConnection()
        {
            isConnectionOpen = true;
            if (!senderConnected)
            {
                Task.Run(SendData);
            }
            if (!receiverConnected)
            {
                Task.Run(ReceiveData);
            }
        }

        private async void SendData()
        {
            var conn = new Grpc.Core.Channel(EmulatorManager.DEFAULT_HOST + ":" + ((EmulatorManager.programType == UserProgram.Type.JAVA) ? DEFAULT_JAVA_PORT : DEFAULT_NATIVE_PORT), Grpc.Core.ChannelCredentials.Insecure);
            var client = new EmulationWriter.EmulationWriterClient(conn);
            while (EmulatorManager.IsTryingToRunRobotCode() && Instance) // Run while robot code is running or until the object stops existing
            {
                try
                {
                    using (var call = client.RobotInputs())
                    {
                        while(EmulatorManager.IsTryingToRunRobotCode() && Instance){
                            await call.RequestStream.WriteAsync(new UpdateRobotInputsRequest
                            {
                                Api = API_VERSION,
                                TargetPlatform = EmulatorManager.programType == UserProgram.Type.JAVA ? TargetPlatform.Java : TargetPlatform.Native,
                                InputData = InputManager.Instance,
                            });
                            senderConnected = true;
                            // Debug.Log("Sending " + InputManager.Instance);
                            await Task.Delay(LOOP_DELAY); // ms
                        }
                    }
                }
                catch (Exception)
                {
                    senderConnected = false;
                    await Task.Delay(ERROR_DELAY); // ms
                }
            }
            using (var call = client.RobotInputs())
            {
                await call.RequestStream.CompleteAsync();
            }
            senderConnected = false;
            isConnectionOpen = false;
        }

        private async Task ReceiveData()
        {
            var conn = new Grpc.Core.Channel(EmulatorManager.DEFAULT_HOST + ":" + ((EmulatorManager.programType == UserProgram.Type.JAVA) ? DEFAULT_JAVA_PORT : DEFAULT_NATIVE_PORT), Grpc.Core.ChannelCredentials.Insecure);
            var client = new EmulationReader.EmulationReaderClient(conn);
            while (EmulatorManager.IsTryingToRunRobotCode() && Instance) // Run while robot code is running or until the object stops existing
            {
                try
                {
                    using (var call = client.RobotOutputs(new RobotOutputsRequest {
                        Api = API_VERSION,
                        TargetPlatform = EmulatorManager.programType == UserProgram.Type.JAVA ? TargetPlatform.Java : TargetPlatform.Native,
                    }))
                    {
                        while (await call.ResponseStream.MoveNext())
                        {
                            receiverConnected = true;
                            OutputManager.Instance = call.ResponseStream.Current.OutputData;
                            // Debug.Log("Received " + OutputManager.Instance);
                        }
                    }
                }
                catch (Exception)
                {
                    receiverConnected = false;
                    await Task.Delay(ERROR_DELAY); // ms
                }
            }
            receiverConnected = false;
            isConnectionOpen = false;
        }

        public bool IsConnectionOpen()
        {
            return isConnectionOpen;
        }

        public void OnApplicationQuit()
        {
            // inputCommander.Send(new StandardMessage.ExitMessage());
            // outputCommander.Send(new StandardMessage.ExitMessage());
            EmulatorManager.KillEmulator();
        }

        public bool IsConnected()
        {
            return senderConnected && receiverConnected;
            //return sender.IsConnected() && receiver.IsConnected();
        }

        /*
        public void SendOutputMessage(IMessage message)
        {
            outputCommander.Send(message);
        }

        public void SendInputMessage(IMessage message)
        {
            inputCommander.Send(message);
        }
        */
    }
}
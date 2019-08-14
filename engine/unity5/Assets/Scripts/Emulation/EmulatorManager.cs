﻿using System;
using Renci.SshNet;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Synthesis.GUI;

namespace Synthesis
{
    public static class EmulatorManager
    {
        // Directory Info
        private static readonly string EMULATION_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "Autodesk" + Path.DirectorySeparatorChar + "Synthesis" + Path.DirectorySeparatorChar + "Emulator";

        private enum OS
        {
            Windows,
            MacOS,
            Linux
        }

#if UNITY_STANDALONE_OSX
        private static readonly OS os = OS.MacOS;
#elif UNITY_STANDALONE_LINUX
        private static readonly OS os = OS.Linux;
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
        private static readonly OS os = OS.Windows;
#endif
        private static readonly string QEMU_DIR = "C:" + Path.DirectorySeparatorChar + "Program Files" + Path.DirectorySeparatorChar + "qemu";

        // File Info
        private static readonly string NATIVE_KERNEL = EMULATION_DIR + Path.DirectorySeparatorChar + "kernel-native";
        private static readonly string ROOTFS_NATIVE = EMULATION_DIR + Path.DirectorySeparatorChar + "rootfs-native.ext4";
        private static readonly string NATIVE_DEVICE_TREE = EMULATION_DIR + Path.DirectorySeparatorChar + "zynq-zed.dtb";

        private static readonly string JAVA_KERNEL = EMULATION_DIR + Path.DirectorySeparatorChar + "kernel-java";
        private static readonly string ROOTFS_JAVA = EMULATION_DIR + Path.DirectorySeparatorChar + "rootfs-java.ext4";

        private static readonly string GRPC_BRIDGE = EMULATION_DIR + Path.DirectorySeparatorChar + "grpc-bridge" + (os == OS.Windows ? ".exe" : (os == OS.MacOS ? "-macos" : ""));
        private static readonly string QEMU_ARM = os == OS.Windows ? (QEMU_DIR + Path.DirectorySeparatorChar + "qemu-system-arm.exe") : "qemu-system-arm";
        private static readonly string QEMU_X86 = os == OS.Windows ? (QEMU_DIR + Path.DirectorySeparatorChar + "qemu-system-x86_64.exe") : "qemu-system-x86_64";

        // SSH Info
        public const string DEFAULT_HOST = "127.0.0.1";
        private const int DEFAULT_SSH_PORT_CPP = 10022;
        private const int DEFAULT_SSH_PORT_JAVA = 10023;

        private const string USER = "lvuser";
        private const string PASSWORD = "";

        // Commands
        private const string REMOTE_LOG_NAME = "/home/lvuser/logs/log.log";

        private const string STOP_COMMAND = "sudo killall frc_program_runner.sh FRCUserProgram java &> /dev/null;";
        private const string START_COMMAND = "nohup /home/lvuser/frc_program_runner.sh </dev/null &> /dev/null &";
        private const string CHECK_EXISTS_COMMAND = "[ -f /home/lvuser/FRCUserProgram ] || [ -f /home/lvuser/FRCUserProgram.jar ]";
        private const string CHECK_RUNNER_RUNNING_COMMAND = "pidof frc_program_runner.sh &> /dev/null";
        private const string CHECK_RUNNING_COMMAND = "pidof FRCUserProgram java &> /dev/null";
        private const string RECEIVE_PRINTS_COMMAND = "tail -F " + REMOTE_LOG_NAME;

        private static SshCommand outputStreamCommand = null;
        private static IAsyncResult outputStreamCommandResult = null;

        // Process Info
        private static System.Diagnostics.Process qemuNativeProcess = null;
        private static System.Diagnostics.Process qemuJavaProcess = null;
        private static System.Diagnostics.Process grpcBridgeProcess = null;

        public static UserProgram.Type programType = UserProgram.Type.JAVA;
        
		// Last connection status
        public static bool UseEmulation = false;
        private static bool VMConnected = false;
        private static bool frcUserProgramPresent = false;
        private static bool isTryingToRunRobotCode = false;
        private static bool isRunningRobotCodeRunner = false;
        private static bool isRunningRobotCode = false;
        private static bool isRobotCodeRestarting = false;

        private static bool updatingStatus = false;

        private static class ClientManager
        {
            public static void Connect()
            {
                if (emulatorType != programType && Instance != null)
                {
                    Close();
                }
                if (Instance == null)
                {
                    emulatorType = programType;
                    Instance = new SshClient(DEFAULT_HOST, (emulatorType == UserProgram.Type.JAVA) ? DEFAULT_SSH_PORT_JAVA : DEFAULT_SSH_PORT_CPP, USER, PASSWORD);
                }
                if (!Instance.IsConnected)
                {
                    try
                    {
                        Instance.Connect();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }

            public static void Close()
            {
                if (Instance != null)
                {
                    if (Instance.IsConnected)
                    {
                        Instance.Disconnect();
                    }
                    Instance.Dispose();
                    Instance = null;
                }
            }

            public static SshClient Instance { get; private set; }

            private static UserProgram.Type emulatorType = UserProgram.Type.JAVA;
        }

        public static bool IsVMInstalled()
        {
            return Directory.Exists(EMULATION_DIR) &&
                Directory.Exists(QEMU_DIR) == (os == OS.Windows) &&
                File.Exists(NATIVE_KERNEL) &&
                File.Exists(ROOTFS_NATIVE) &&
                File.Exists(NATIVE_DEVICE_TREE) &&
                File.Exists(JAVA_KERNEL) &&
                File.Exists(ROOTFS_JAVA) &&
                File.Exists(GRPC_BRIDGE) &&
                File.Exists(QEMU_ARM) &&
                File.Exists(QEMU_X86);
        }

        public static bool IsVMRunning()
        {
            if (qemuNativeProcess != null && qemuNativeProcess.HasExited)
                qemuNativeProcess = null;
            if (qemuJavaProcess != null && qemuJavaProcess.HasExited)
                qemuJavaProcess = null;
            if (grpcBridgeProcess != null && grpcBridgeProcess.HasExited)
                grpcBridgeProcess = null;

            return qemuNativeProcess != null && qemuJavaProcess != null && grpcBridgeProcess != null;
        }

        public static bool IsVMConnected()
        {
            return VMConnected;
        }

        public static bool IsFRCUserProgramPresent()
        {
            return frcUserProgramPresent;
        }

        public static bool IsTryingToRunRobotCode()
        {
            return isTryingToRunRobotCode || isRunningRobotCode;
        }

        public static bool IsRunningRobotCodeRunner()
        {
            return isRunningRobotCodeRunner;
        }

        public static bool IsRunningRobotCode()
        {
            return isRunningRobotCode;
        }

        public static bool IsRobotCodeRestarting()
        {
            return isRobotCodeRestarting;
        }

        public static bool StartEmulator()
        {
            if (IsVMRunning())
            {
                throw new Exception("Emulator already running");
            }
            bool exception = false;
            try
            {
                qemuNativeProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = QEMU_ARM,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    Arguments = " -machine xilinx-zynq-a9 -cpu cortex-a9 -m 2048 -kernel " + NATIVE_KERNEL + " -dtb " + NATIVE_DEVICE_TREE +" " +
                        "-display none -serial null -serial mon:stdio -append \"console=ttyPS0,115200 earlyprintk root=/dev/mmcblk0 rw\" " +
                        "-net user,hostfwd=tcp::" + DEFAULT_SSH_PORT_CPP + "-:22,hostfwd=tcp::" + EmulatorNetworkConnection.DEFAULT_NATIVE_PORT + "-:" + EmulatorNetworkConnection.DEFAULT_PORT + "," +
                        "hostfwd=tcp::2354-:2354 -net nic -sd " + ROOTFS_NATIVE,
                    Verb = "runas"
                });

                qemuJavaProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = QEMU_X86,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    Arguments = " -m 2048 -kernel " + JAVA_KERNEL + " -nographic -append \"console=ttyPS0 root=/dev/sda rw\" " +
                        "-net user,hostfwd=tcp::" + DEFAULT_SSH_PORT_JAVA + "-:22,hostfwd=tcp::" + EmulatorNetworkConnection.DEFAULT_JAVA_PORT + "-:" + EmulatorNetworkConnection.DEFAULT_PORT + " " +
                        "-net nic -hda " + ROOTFS_JAVA,
                    Verb = "runas"
                });

                grpcBridgeProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = GRPC_BRIDGE,
                    Verb = "runas"
                });

                AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
                {
                    KillEmulator();
                };
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                exception = true;
            }
            return !exception && IsVMRunning();
        }

        public static void KillEmulator()
        {
            if (VMConnected)
            {
                ClientManager.Close();
            }

            StopUpdatingStatus();

            VMConnected = false;
            frcUserProgramPresent = false;
            isTryingToRunRobotCode = false;
            isRunningRobotCodeRunner = false;

            if (IsVMRunning())
            {
                qemuNativeProcess.Kill();
                qemuNativeProcess = null;
                qemuJavaProcess.Kill();
                qemuJavaProcess = null;
                grpcBridgeProcess.Kill();
                grpcBridgeProcess = null;
            }
        }

        public static async void GracefulExit()
        {
            // outputCommander.Send(new StandardMessage.ExitMessage());
            if (IsVMConnected())
            {
                if (IsRunningRobotCodeRunner())
                {
                    await StopRobotCode();
                }
            }
            if (IsVMRunning())
            {
                KillEmulator();
            }
        }

        private static void StatusUpdater()
        {
            var lastProgramType = programType;
            SshClient client = null;
            while (updatingStatus)
            {
                if (programType != lastProgramType)
                {
                    lastProgramType = programType;
                    if(client != null)
                    {
                        client.Disconnect();
                        client.Dispose();
                        client = null;
                    }
                }
                try
                {
                    if (client == null)
                    {
                        client = new SshClient(DEFAULT_HOST, lastProgramType == UserProgram.Type.JAVA ? DEFAULT_SSH_PORT_JAVA : DEFAULT_SSH_PORT_CPP, USER, PASSWORD);
                        client.Connect();
                    }

                    VMConnected = client.IsConnected;
                    if (VMConnected)
                    {
                        frcUserProgramPresent = client.RunCommand(CHECK_EXISTS_COMMAND).ExitStatus == 0;
                        if (frcUserProgramPresent)
                        {
                            isRunningRobotCodeRunner = client.RunCommand(CHECK_RUNNER_RUNNING_COMMAND).ExitStatus == 0;
                            isRunningRobotCode = client.RunCommand(CHECK_RUNNING_COMMAND).ExitStatus == 0;
                            if (isRunningRobotCode && !EmulatorNetworkConnection.Instance.IsConnectionOpen())
                            {
                                EmulatorNetworkConnection.Instance.OpenConnection();
                            }
                            if (isRunningRobotCode)
                            {
                                Thread.Sleep(3000); // ms
                            }
                            else
                            {
                                Thread.Sleep(1000); // ms
                            }
                        }
                        else
                        {
                            isRunningRobotCodeRunner = false;
                            isRunningRobotCode = false;
                            Thread.Sleep(1000); // ms
                        }
                    }
                    else
                    {
                        frcUserProgramPresent = false;
                        isRunningRobotCodeRunner = false;
                        isRunningRobotCode = false;
                        Thread.Sleep(3000); // ms
                        client.Connect();
                    }
                }
                catch
                {
                    frcUserProgramPresent = false;
                    isRunningRobotCodeRunner = false;
                    isRunningRobotCode = false;
                    Thread.Sleep(3000); // ms
                }
            }
        }

        public static void StartUpdatingStatus()
        {
            if (!updatingStatus)
            {
                updatingStatus = true;
                Task.Run(StatusUpdater);
            }
        }

        public static void StopUpdatingStatus()
        {
            updatingStatus = false;
        }

        public static Task<bool> SCPFileSender(UserProgram userProgram, bool autorun = true)
        {
            return Task.Run(async () =>
            {
                try
                {
                    isRobotCodeRestarting = true; // Prevent code from auto-restarting until this finishes
                    if (IsRunningRobotCodeRunner() || IsTryingToRunRobotCode())
                    {
                        await StopRobotCode();
                    }
                    if (IsRobotOutputStreamGood())
                    {
                        await CloseRobotOutputStream();
                    }

                    programType = userProgram.ProgramType;
                    ClientManager.Connect();
                    ClientManager.Instance.RunCommand("rm -rf FRCUserProgram FRCUserProgram.jar");
                    frcUserProgramPresent = false;
                    using (ScpClient scpClient = new ScpClient(DEFAULT_HOST, programType == UserProgram.Type.JAVA ? DEFAULT_SSH_PORT_JAVA : DEFAULT_SSH_PORT_CPP, USER, PASSWORD))
                    {
                        scpClient.Connect();
                        using (Stream localFile = File.OpenRead(userProgram.FullFileName))
                        {
                            scpClient.Upload(localFile, "/home/lvuser/" + userProgram.TargetFileName);
                            frcUserProgramPresent = true;
                        }
                        scpClient.Disconnect();
                    }
                    if (UseEmulation && autorun)
                    {
                        await RestartRobotCode();
                    }
                    else
                    {
                        isRobotCodeRestarting = false;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                    return false;
                }
                return true;
            });
        }

        public static Task StopRobotCode()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (isRunningRobotCodeRunner || isRunningRobotCode)
                    {
                        ClientManager.Connect();
                        ClientManager.Instance.RunCommand(STOP_COMMAND);
                    }
                    isTryingToRunRobotCode = false;
                    isRunningRobotCodeRunner = false;
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            });
        }

        public static Task RestartRobotCode()
        {
            return Task.Run(() =>
            {
                isRobotCodeRestarting = true;

                try
                {
                    ClientManager.Connect();
                    if (isRunningRobotCodeRunner)
                    {
                        isTryingToRunRobotCode = false; // To reset the gRPC connection
                        isRunningRobotCodeRunner = false;
                        ClientManager.Instance.RunCommand(STOP_COMMAND + "&&" + START_COMMAND);
                    }
                    else
                    {
                        ClientManager.Instance.RunCommand(START_COMMAND);
                    }
                    isTryingToRunRobotCode = true;
                    isRunningRobotCodeRunner = true;
                    EmulatorNetworkConnection.Instance.OpenConnection();
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
                isRobotCodeRestarting = false;
            });
        }

        public static bool IsRobotOutputStreamGood()
        {
            return outputStreamCommand != null && !outputStreamCommandResult.IsCompleted;
        }

        public static Task CloseRobotOutputStream()
        {
            return Task.Run(() =>
            {
                if (outputStreamCommand != null)
                {
                    try
                    {
                        outputStreamCommand.CancelAsync();
                        outputStreamCommand.Dispose();
                        outputStreamCommand = null;
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.ToString());
                    }
                }
            });
        }

        public static StreamReader CreateRobotOutputStream()
        {
            if (outputStreamCommand == null) {
                try
                {
                    ClientManager.Connect();
                    outputStreamCommand = ClientManager.Instance.CreateCommand(RECEIVE_PRINTS_COMMAND);
                    outputStreamCommandResult = outputStreamCommand.BeginExecute();
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                    outputStreamCommand = null;
                }
            }
            return new StreamReader(outputStreamCommand.OutputStream);
        }

        public static Task FetchLogFile()
        {
            return Task.Run(() =>
            {
                string folder = SFB.StandaloneFileBrowser.OpenFolderPanel("Log file destination", "C:\\", false);
                if (folder == null)
                {
                    Debug.Log("No folder selected for log file destination");
                }
                else
                {
                    try
                    {
                        using (ScpClient client = new ScpClient(DEFAULT_HOST, programType == UserProgram.Type.JAVA ? DEFAULT_SSH_PORT_JAVA : DEFAULT_SSH_PORT_CPP, USER, PASSWORD))
                        {
                            client.Connect();
                            Stream localLogFile = File.Create(folder + "/log.log");
                            client.Download(REMOTE_LOG_NAME, localLogFile);
                            localLogFile.Close();
                            client.Disconnect();
                        }
                    }
                    catch (Exception)
                    {
                        UserMessageManager.Dispatch("Failed to download log file", EmulationWarnings.WARNING_DURATION);
                    }
                }
            });
        }
    }
}
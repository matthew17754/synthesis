using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading.Message
{
    public class Default
    {
        public class Task
        {
            public const string Start = "START";
            public const string Resume = "RESUME";
            public const string Pause = "PAUSE";
            public const string Stop = "STOP";
            public const string Exit = "EXIT";
            public class StartMessage : IMessage
            {
                public static implicit operator string(StartMessage m) => m.GetName();
                public string GetName()
                {
                    return "START";
                }
            }
            public class ResumeMessage : IMessage
            {
                public static implicit operator string(ResumeMessage m) => m.GetName();
                public string GetName()
                {
                    return "RESUME";
                }
            }
            public class PauseMessage : IMessage
            {
                public static implicit operator string(PauseMessage m) => m.GetName();
                public string GetName()
                {
                    return "PAUSE";
                }
            }
            public class StopMessage : IMessage
            {
                public static implicit operator string(StopMessage m) => m.GetName();
                public string GetName()
                {
                    return "STOP";
                }
            }
            public class ExitMessage : IMessage
            {
                public static implicit operator string(ExitMessage m) => m.GetName();
                public string GetName()
                {
                    return "EXIT";
                }
            }
        }
        public class State
        {
            public const string ThreadStopped = "THREAD_STOPPED";
            public const string ConnectionFailure = "CONNECTION_FAILURE";
            public const string GracefulExit = "GRACEFUL_EXIT";
            public const string UnhandledException = "UNHANDLED_EXCEPTION";
            public class ThreadStoppedMessage : IMessage
            {
                public static implicit operator string(ThreadStoppedMessage m) => m.GetName();
                public string GetName()
                {
                    return "THREAD_STOPPED";
                }
            }
            public class ConnectionFailureMessage : IMessage
            {
                public static implicit operator string(ConnectionFailureMessage m) => m.GetName();
                public string GetName()
                {
                    return "CONNECTION_FAILURE";
                }
            }
            public class GracefulExitMessage : IMessage
            {
                public static implicit operator string(GracefulExitMessage m) => m.GetName();
                public string GetName()
                {
                    return "GRACEFUL_EXIT";

                }
            }
            public class UnhandledExceptionMessage : IMessage
            {
                public static implicit operator string(UnhandledExceptionMessage m) => m.GetName();
                public string GetName()
                {
                    return "GRACEFUL_EXIT";

                }
            }

        }
    }
}

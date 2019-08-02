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
        }
        public class State
        {
            public const string ThreadStopped = "THREAD_STOPPED";
            public const string ConnectionFailure = "CONNECTION_FAILURE";
            public const string GracefulExit = "GRACEFUL_EXIT";
            public const string UnhandledException = "UNHANDLED_EXCEPTION";

        }
    }
}

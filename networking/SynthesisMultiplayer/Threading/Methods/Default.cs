using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading.Methods
{
    public class Default
    {
        internal class Internal
        {
            public const string OnStart = "ON_START";
            public const string OnPause = "ON_PAUSE";
            public const string OnResume = "ON_RESUME";
            public const string OnStop = "ON_STOP";
            public const string OnExit = "ON_EXIT";
        }
        public class Task
        {
            public const string Start = "START";
            public const string Resume = "RESUME";
            public const string Pause = "PAUSE";
            public const string Stop = "STOP";
            public const string Exit = "EXIT";
            public const string GetStatus = "GET_STATUS";
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

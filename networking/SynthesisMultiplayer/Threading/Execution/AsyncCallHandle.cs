using System;
using System.Collections.Generic;
using System.Threading;

namespace SynthesisMultiplayer.Threading.Execution
{
    public class AsyncCallHandle : IDisposable
    {
        public enum FaultType
        {
            None,
            Exception
        }
        dynamic result;
        bool ready;
        private readonly int methodCallWaitPeriod = 50;

        public dynamic Result
        {
            get
            {
                Wait();
                return result;
            }
            set
            {
                result = value;
                ready = true;
            }
        }
        public Queue<dynamic> Arguments { get; set; }
        public bool Ready
        {
            get
            {
                return ready || result != null;
            }
        }

        public bool Fault { get; set; }
        public FaultType Type { get; set; }

        public AsyncCallHandle(params dynamic[] Args)
        {
            Arguments = new Queue<dynamic>();
            Fault = false;
            Type = FaultType.None;
            ready = false;
            result = null;
            foreach (var arg in Args)
                Arguments.Enqueue(arg);
        }

        public void Done() => ready = true;
        public void Wait()
        {
            while (!Ready)
            {
                if (Fault)
                {

                    return;
                }
                Thread.Sleep(methodCallWaitPeriod);
            }

        }
        public void Dispose()
        {
            Arguments.Clear();
        }
    }
}

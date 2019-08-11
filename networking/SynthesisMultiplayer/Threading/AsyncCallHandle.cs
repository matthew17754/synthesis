using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public class AsyncCallHandle : IDisposable
    {
        public enum FaultType
        {
            None,
            Exception
        }

        public Queue<dynamic> Arguments { get; set; }
        public bool Ready { get; set; }
        public dynamic Result { get; set; }
        public bool Fault { get; set; }
        public FaultType Type { get; set; }

        public AsyncCallHandle(params dynamic[] Args)
        {
            Arguments = new Queue<dynamic>();
            Ready = false;
            Fault = false;
            Type = FaultType.None;
            Result = null;
            foreach (var arg in Args)
                Arguments.Enqueue(arg);
        }

        public void Dispose()
        {
            Arguments.Clear();
        }

        public void Done()
        {
            Ready = true;
        }
    }
}

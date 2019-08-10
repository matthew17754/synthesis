using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public enum ManagedTaskStatus
    {
        Created,
        Initialized,
        Ready,
        Running,
        Fault,
        Canceled,
        Completed
    }
}

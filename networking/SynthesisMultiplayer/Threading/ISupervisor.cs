using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public enum RestartStrategy
    {
        OneForOne,
        OneForAll,
        RestforOne
    }

    interface ISupervisor
    {
        (IManagedTask, Task) GetChild(string name);
        void RestartChild(string name);
        string SpawnChild(IManagedTask taskObject, string name = "");
        
    }
}

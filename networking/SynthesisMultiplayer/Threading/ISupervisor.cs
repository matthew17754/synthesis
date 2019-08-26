using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading.Execution
{
    public interface ISupervisor : IManagedTask
    {
        Guid StartChild(IManagedTask task, string name = null);
        void TerminateChild(Guid childId);
        void TerminateChild(string childName);
        int CountChildren();
        List<(Guid taskId, Type type)> Children();
    }

    public static class ISupervisorMethods
    {
    }
}

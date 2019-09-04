using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiplayer.Actor
{
    public interface ISupervisor : IActor
    {
        Guid StartChild(IActor task, string name = null);
        void TerminateChild(Guid childId);
        void TerminateChild(string childName);
        int CountChildren();
        List<(Guid taskId, Type type)> Children();
    }

    public static class ISupervisorMethods
    {
    }
}

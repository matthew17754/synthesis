using SynthesisMultiplayer.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public class ManagedTaskHelper
    {
        public static Guid Start(IManagedTask task, string name = null) => ManagedTaskRegistry.StartTask(task, name, new TaskContext());
        public static void Restart(Guid taskId, bool doRestoreState = true) => ManagedTaskRegistry.RestartTask(taskId, doRestoreState);
        public static IManagedTask GetTask(Guid taskId) => ManagedTaskRegistry.GetTaskObject(taskId);
        public static IManagedTask GetTask(string taskName) => ManagedTaskRegistry.GetTaskObject(taskName);
    }
}

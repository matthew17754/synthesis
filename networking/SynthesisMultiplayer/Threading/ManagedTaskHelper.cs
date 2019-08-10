using SynthesisMultiplayer.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public class ManagedTaskHelper
    {
        internal static Task Run(IManagedTask task, ITaskContext context, Dictionary<string, dynamic> state = null)
        {
            ITaskContext taskContext;
            task.Initialize();
            if (state != null)
                task.RestoreState(state);
            return Task.Factory.StartNew((c) =>
            {
                if (c != null)
                    taskContext = (ITaskContext)c;
                else
                    taskContext = new TaskContext();
                task.OnStart(context: taskContext);
                while (true)
                {
                    task.OnMessage(context: taskContext);
                    if (!task.IsAlive())
                        return;
                    if (task.IsPaused())
                        Thread.Sleep(50);
                    if (task.GetStatus() == ManagedTaskStatus.Canceled)
                        return;
                    else
                        task.OnCycle(context: taskContext);
                }
            }, context);
        }
        public static Guid Start(IManagedTask task, string name = null)
        {
            return ManagedTaskRegistry.StartTask(task, name, new TaskContext());
        }

        public static void Restart(Guid taskId, bool doRestoreState = true) => ManagedTaskRegistry.RestartTask(taskId, doRestoreState);
        public static IManagedTask GetTask(Guid taskId) => ManagedTaskRegistry.GetTaskObject(taskId);
    }
}

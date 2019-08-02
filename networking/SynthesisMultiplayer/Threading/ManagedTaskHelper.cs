using System;
using System.Threading;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public class ManagedTaskHelper
    {
        public static Task Run(IManagedTask task, ITaskContext context)
        {
            ITaskContext taskContext;
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
                    else
                        task.OnCycle(context: taskContext);
                }
            }, context);
        }
    }
}

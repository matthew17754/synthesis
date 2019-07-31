using System;
using System.Threading;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public class ManagedTaskHelper
    {
        public static Task Run(IManagedTask task, ITaskContext context)
        {
            return Task.Factory.StartNew((c) =>
            {
                ITaskContext taskContext;
                if (c != null)
                    taskContext = (ITaskContext)c;
                else
                    taskContext = new TaskContextBase();
                task.OnStart(ref taskContext);
                while (true)
                {
                    task.OnMessage(ref taskContext);
                    if (!task.IsAlive())
                        return;
                    if (task.IsPaused())
                        Thread.Sleep(50);
                    else
                        task.OnCycle(ref taskContext);
                }

            }, context);
        }
    }
}

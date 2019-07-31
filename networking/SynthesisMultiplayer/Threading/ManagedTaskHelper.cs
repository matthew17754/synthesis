using System.Threading;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public class ManagedTaskHelper
    {
        public static Task Run(IManagedTask task, ITaskContext context)
        {
            return Task.Factory.StartNew(() =>
            {
                task.OnStart(context);
                while (true)
                {
                    task.OnMessage(context);
                    if (!task.IsAlive())
                        return;
                    if (task.IsPaused())
                        Thread.Sleep(50);
                    else
                        task.OnCycle(context);
                }

            });
        }
    }
}

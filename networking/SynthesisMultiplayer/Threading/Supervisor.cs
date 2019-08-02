using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SynthesisMultiplayer.Threading.Message;
namespace SynthesisMultiplayer.Threading
{

    class Supervisor : ManagedTask, ISupervisor
    {
        bool disposed = false; // To detect redundant calls
        readonly RestartStrategy strategy;
        Dictionary<string, (IManagedTask, Task)> Children;


        public Supervisor(params IManagedTask[] children)
        {
            Children = new Dictionary<string, (IManagedTask, Task)>();
        }

        public (IManagedTask, Task) GetChild(string name)
        {
            if (!Children.ContainsKey(name))
            {
                throw new Exception("No task named '" + name + "' was found.");
            }
            return Children[name];
        }

        public override void OnStart(ITaskContext context, AsyncCallHandle? handle)
        {

            base.OnStart(context, handle);
        }

        public void RestartChild(string name)
        {
            var (taskObject, task) = GetChild(name);
            taskObject.Do(Default.Task.Exit).Wait();
            if (taskObject.GetState() != Default.State.GracefulExit)
            {
                // This case is impossible right now, but still good to check
            }

        }

        public string SpawnChild(IManagedTask taskObject, string name = "")
        {
            var task = ManagedTaskHelper.Run(taskObject, new TaskContext());
            if (name == "")
            {
                var guid = new Guid();
                name = guid.ToString();
            }
            Children[name] = (taskObject, task);
            return name;
        }

        protected void PollTasks()
        {
            foreach (var child in Children)
            {
                var (taskObject, task) = child.Value;
                switch (task.Status)
                {
                    case TaskStatus.Created:
                    case TaskStatus.Running:
                        break;
                    case TaskStatus.RanToCompletion:
                        if (taskObject.GetState() == Default.State.GracefulExit)
                            break;
                        HandleRestart();
                        break;
                    case TaskStatus.Canceled:
                    case TaskStatus.Faulted:
                        task.Exception.Handle(ex =>
                        {
                            return false;
                        });
                        break;
                    default:
                        break;
                }
            }
        }
        void HandleRestart() { }


        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                }
                disposed = true;
            }
        }
    }
}

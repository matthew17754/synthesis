using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SynthesisMultiplayer.Threading.Message;
namespace SynthesisMultiplayer.Threading
{

    class Supervisor : ISupervisor
    {
        public Supervisor()
        {
            Children = new Dictionary<string, (IManagedTask, Task)>();
        }

        readonly RestartStrategy strategy;
        Dictionary<string, (IManagedTask, Task)> Children;

        public (IManagedTask, Task) GetChild(string name)
        {
            if(!Children.ContainsKey(name))
            {
                throw new Exception("No task named '" + name + "' was found.");
            }
            return Children[name];
        }

        public void RestartChild(string name)
        {
            var (taskObject, task) = GetChild(name);
            taskObject.SendMessage(new Default.Task.ExitMessage());
            if (taskObject.GetMessage().GetName() != Default.State.GracefulExit)
            {
                // This case is impossible right now, but still good to check
            }
            
        }

        public string SpawnChild(IManagedTask taskObject, string name = "")
        {
            var task = ManagedTaskHelper.Run(taskObject, new TaskContextBase());
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
            foreach(var child in Children)
            {
                var (taskObject, task) = child.Value;
                switch(task.Status)
                {
                    case TaskStatus.Created:
                    case TaskStatus.Running:
                        break;
                    case TaskStatus.RanToCompletion:
                        if(taskObject.GetMessage().GetName() == Default.State.GracefulExit)
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

        public bool IsAlive()
        {
            throw new NotImplementedException();
        }

        public bool IsPaused()
        {
            throw new NotImplementedException();
        }

        public void RegisterCallback(string name, ManagedTaskCallback callback)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(IMessage message)
        {
            throw new NotImplementedException();
        }

        public IMessage GetMessage()
        {
            throw new NotImplementedException();
        }

        public void OnMessage(ref ITaskContext context)
        {
            throw new NotImplementedException();
        }

        public void OnCycle(ref ITaskContext context)
        {
            throw new NotImplementedException();
        }

        public void OnStart(ref ITaskContext context)
        {
            throw new NotImplementedException();
        }

        public void OnResume(ref ITaskContext context)
        {
            throw new NotImplementedException();
        }

        public void OnPause(ref ITaskContext context)
        {
            throw new NotImplementedException();
        }

        public void OnStop(ref ITaskContext context)
        {
            throw new NotImplementedException();
        }

        public void OnExit(ref ITaskContext context)
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Supervisor() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

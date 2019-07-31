using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{

    public delegate void ManagedTaskCallback(ITaskContext context);
    public interface IManagedTask : IDisposable
    {
        bool IsAlive();
        bool IsPaused();

        void RegisterCallback(string name, ManagedTaskCallback callback);

        void OnStart(ITaskContext context);
        void OnResume(ITaskContext context);
        void OnMessage(ITaskContext context);
        void OnCycle(ITaskContext context);
        void OnPause(ITaskContext context);
        void OnStop(ITaskContext context);
        void OnExit(ITaskContext context);
        void SendMessage(IMessage message);
    }
}

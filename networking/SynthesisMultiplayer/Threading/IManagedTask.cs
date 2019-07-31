using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{

    public delegate void ManagedTaskCallback(ref ITaskContext context);
    public interface IManagedTask : IDisposable
    {
        bool IsAlive();
        bool IsPaused();

        void RegisterCallback(string name, ManagedTaskCallback callback);

        void SendMessage(IMessage message);
        IMessage GetMessage();

        void OnMessage(ref ITaskContext context);
        void OnCycle(ref ITaskContext context);

        void OnStart(ref ITaskContext context);
        void OnResume(ref ITaskContext context);

        void OnPause(ref ITaskContext context);
        void OnStop(ref ITaskContext context);
        void OnExit(ref ITaskContext context);
    }
}

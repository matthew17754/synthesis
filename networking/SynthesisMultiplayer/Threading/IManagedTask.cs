using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{

    public delegate void ManagedTaskCallback(ITaskContext context, AsyncCallHandle handle = null);
    public interface IManagedTask : IDisposable
    {
        bool IsAlive();
        bool IsPaused();

        string GetState();

        Task<dynamic> Call(string method, params dynamic[] args);
        Task Do(string method, params dynamic[] args);

        Dictionary<string, dynamic> DumpState(Dictionary<string, dynamic> currentState);
        void RestoreState(Dictionary<string, dynamic> state);

        ManagedTaskStatus GetStatus();

        void Initialize();
        bool IsReady();

        void OnMessage(ITaskContext context, AsyncCallHandle handle = null);
        void OnCycle(ITaskContext context, AsyncCallHandle handle = null);

        void OnStart(ITaskContext context, AsyncCallHandle handle = null);
        void OnResume(ITaskContext context, AsyncCallHandle handle = null);

        void OnPause(ITaskContext context, AsyncCallHandle handle = null);
        void OnStop(ITaskContext context, AsyncCallHandle handle = null);
        void OnExit(ITaskContext context, AsyncCallHandle handle = null);

        void Cancel();
    }
}

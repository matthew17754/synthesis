using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{

    public delegate void ManagedTaskCallback(ITaskContext context, AsyncCallHandle? handle = null);
    public interface IManagedTask : IDisposable
    {
        bool IsAlive();
        bool IsPaused();

        string GetState();

        Task<dynamic> Call(string method, params dynamic[] args);
        Task Do(string method, params dynamic[] args);

        void OnMessage(ITaskContext context, AsyncCallHandle? handle = null);
        void OnCycle(ITaskContext context, AsyncCallHandle? handle = null);

        void OnStart(ITaskContext context, AsyncCallHandle? handle = null);
        void OnResume(ITaskContext context, AsyncCallHandle? handle = null);

        void OnPause(ITaskContext context, AsyncCallHandle? handle = null);
        void OnStop(ITaskContext context, AsyncCallHandle? handle = null);
        void OnExit(ITaskContext context, AsyncCallHandle? handle = null);
    }
}

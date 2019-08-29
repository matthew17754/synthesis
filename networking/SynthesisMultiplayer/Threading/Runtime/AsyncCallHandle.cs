using Multiplayer.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Threading;

namespace Multiplayer.Threading.Runtime
{
    public class AsyncCallHandle : IDisposable
    {
        public enum FaultType
        {
            None,
            Exception
        }
        dynamic result;
        bool ready;
        private readonly int methodCallWaitPeriod = 50;
        private List<int> DefaultedArgs;
        public dynamic Result
        {
            [DebuggerStepThrough]
            get
            {
                Wait();
                return result;
            }
            set
            {
                result = value;
                ready = true;
            }
        }
        public Queue<dynamic> Arguments { get; set; }
        public bool Ready
        {
            get
            {
                return ready || result != null;
            }
        }

        public bool Fault { get; set; }
        public FaultType Type { get; set; }

        public AsyncCallHandle(params dynamic[] Args)
        {
            
            Arguments = new Queue<dynamic>();
            Fault = false;
            Type = FaultType.None;
            ready = false;
            result = null;
            foreach (var arg in Args)
                Arguments.Enqueue(arg);
        }


        public AsyncCallHandle(CallbackInfo info, params dynamic[] Args)
        {
            
            Arguments = new Queue<dynamic>();
            DefaultedArgs = new List<int>();
            Fault = false;
            Type = FaultType.None;
            ready = false;
            result = null;
            var StagingArgs = new dynamic[info.Arguments.Count];
            for (int i = 0; i < info.Arguments.Count; ++i)
                StagingArgs[i] = new NoneType { };
            var skippedIndicies = new List<int>();
            var idx = 0;
            foreach(var arg in Args)
            {
                if (((object)arg).GetType() == typeof((string, dynamic)))
                {
                    var (argName, value) = ((ValueTuple<string, dynamic>)arg);
                    if (!info.ContainsArgument(argName))
                    {
                        throw new Exception($"No argument named: {argName} for method {info.Name}");
                    }
                    StagingArgs[info.ArgumentIndex(argName)] = value;
                }
            }
            foreach (var arg in Args)
            {
                while (((object)StagingArgs[idx]).GetType() != typeof(NoneType))
                {
                    ++idx;
                }
                StagingArgs[idx] = arg;
                ++idx;
            }
            idx = 0;
            foreach(var arg in StagingArgs)
            {
                if (((object)arg).GetType() == typeof(NoneType))
                {
                    if (info.ArgumentAt(idx).Attributes.Contains(RuntimeArgumentAttributes.HasDefault))
                    {
                        StagingArgs[idx] = info.ArgumentAt(idx).DefaultValue;
                    }
                    else
                    {
                        var argumentData = info.ArgumentAt(idx);
                        if(argumentData.Type.IsValueType)
                        {
                            StagingArgs[idx] = Activator.CreateInstance(argumentData.Type);
                        }
                        else
                        {
                            StagingArgs[idx] = null;
                        }
                    }
                }
                ++idx;
            }
            StagingArgs.ToList().ForEach(a => Arguments.Enqueue(a));
        }

        public void Done() => ready = true;
        public void Wait()
        {
            while (!Ready)
            {
                if (Fault)
                {

                    return;
                }
                Thread.Sleep(methodCallWaitPeriod);
            }
        }
        public void Dispose()
        {
            Arguments.Clear();
        }
    }
}

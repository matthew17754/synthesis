using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public interface ITaskContext : IDisposable
    {
        dynamic GetObject(string key);
        void PutObject(string key, dynamic value);
    }
}

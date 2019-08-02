using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Attribute
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class Callback : System.Attribute
    {
        public string Name { get; set; }
        public string MethodName { get; set; }
        public Callback(string name = null, string methodName = null)
        {
            Name = name;
            MethodName = methodName;
        }
    }
}

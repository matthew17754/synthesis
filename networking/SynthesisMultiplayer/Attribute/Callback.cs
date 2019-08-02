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
        public Type CallbackMessageType;
        public Callback(string name, Type callbackMessageType)
        {
            Name = name;
            CallbackMessageType = callbackMessageType;
        }
    }
}

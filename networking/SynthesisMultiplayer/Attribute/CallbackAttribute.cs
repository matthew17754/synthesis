using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Attribute
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CallbackAttribute : System.Attribute
    {
        public string Name { get; set; }
        public List<string> ArgumentNames { get; set; }
        public CallbackAttribute(string name = null, params string[] argNames)
        {
            Name = name;
            ArgumentNames = new List<string>();
            if(!argNames.SequenceEqual(argNames.Distinct().ToArray()))
            {
                throw new Exception("duplicate arg names");
            }
            ArgumentNames.AddRange(argNames);
        }
    }
}

using SynthesisMultiplayer.Threading.Runtime;
using System;
using System.Linq;

namespace SynthesisMultiplayer.Attribute
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ArgumentAttribute : System.Attribute
    {
        public string Name { get; set; }
        public RuntimeArgumentAttributes[] Attributes { get; set; }
        public Type Type;
        public object DefaultValue;

        public ArgumentAttribute(string name, Type type, object defaultValue = null, params RuntimeArgumentAttributes[] attr)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            Attributes = attr.Distinct().ToArray();
        }
    }
}

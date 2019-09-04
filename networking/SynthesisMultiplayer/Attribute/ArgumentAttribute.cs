using Multiplayer.Actor.Runtime;
using System;
using System.Linq;

namespace Multiplayer.Attribute
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ArgumentAttribute : System.Attribute
    {
        public string Name { get; set; }
        public ActorCallbackArgumentAttributes[] Attributes { get; set; }
        public Type Type;
        public object DefaultValue;

        public ArgumentAttribute(string name, Type type, object defaultValue = null, params ActorCallbackArgumentAttributes[] attr)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            Attributes = attr.Distinct().ToArray();
        }
    }
}

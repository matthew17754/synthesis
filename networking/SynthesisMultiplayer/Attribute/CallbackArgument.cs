using SynthesisMultiplayer.Threading.Execution;
using SynthesisMultiplayer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SynthesisMultiplayer.Threading.Execution.AsyncCallHandle;

namespace SynthesisMultiplayer.Attribute
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CallbackArgument : System.Attribute
    {
        // [CallbackArgument(name: "Name", Attributes:]
        public string Name { get; set; }
        public ArgumentAttributes[] Attributes { get; set; }
        public dynamic DefaultValue { get; set; } = new NoneType();
        public CallbackArgument(string name, dynamic defaultValue = null, params ArgumentAttributes[] attr)
        {
            Name = name;
            if(attr.Contains(ArgumentAttributes.HasDefaultValue))
                DefaultValue = DefaultValue;
            Attributes = attr;
        }
    }
}

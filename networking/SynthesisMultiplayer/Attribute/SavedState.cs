using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Attribute
{
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SavedState : System.Attribute
    {
        public string Name { get; set; }
        public SavedState(string name = null)
        {
            Name = name;
        }
    }
}

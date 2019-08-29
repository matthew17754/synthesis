using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiplayer.Attribute
{
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SavedStateAttribute : System.Attribute
    {
        public string Name { get; set; }
        public SavedStateAttribute(string name = null)
        {
            Name = name;
        }
    }
}

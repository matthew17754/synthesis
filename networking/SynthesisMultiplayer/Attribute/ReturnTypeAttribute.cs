using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiplayer.Attribute
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ReturnTypeAttribute : System.Attribute
    {
        Type Type { get; set; }
        public ReturnTypeAttribute(Type type)
        {
            Type = type;
        }
    }
}

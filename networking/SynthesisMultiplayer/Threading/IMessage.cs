using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    // This class exists due to C# not having the equivilent of an erlang/elixir atom type, but is functionally equivilent
    public interface IMessage
    {
        string GetName();
    }
}

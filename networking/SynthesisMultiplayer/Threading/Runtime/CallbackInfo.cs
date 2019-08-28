using SynthesisMultiplayer.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading.Runtime
{
    public class CallbackInfo
    {
        public string Name { get; private set; }
        public List<ArgumentAttribute> Arguments { get; private set; }
        public ReturnTypeAttribute ReturnType { get; private set; }
        public CallbackInfo(string name, List<ArgumentAttribute> args, ReturnTypeAttribute returnType = null)
        {
            Name = name;
            Arguments = args;
            ReturnType = returnType;
        }
        public int RequiredParameters
        {
            get
            {
                return Arguments
                    .Where(a => !a.Attributes.Contains(RuntimeArgumentAttributes.Optional) && !a.Attributes.Contains(RuntimeArgumentAttributes.Nullable))
                    .Count();
            }
        }
        // Horrible idea. Fix later
        public bool ContainsArgument(string s)
        {
            foreach (var arg in Arguments)
            {
                if (arg.Name == s)
                {
                    return true;
                }
            }
            return false;
        }

        public int ArgumentIndex(string s)
        {
            var idx = 0;
            foreach (var arg in Arguments)
            {
                if (arg.Name == s)
                {
                    return idx;
                }
                ++idx;
            }
            return -1;
        }

        public ArgumentAttribute ArgumentAt(int i)
        {
            return Arguments[i];
        }

        public bool ValidateArguments(AsyncCallHandle handle)
        {
            Func<dynamic, Type, bool> typeCheck = (a, t) => ((Object)a).GetType() == t;
            if (RequiredParameters > handle.Arguments.Count)
                throw new Exception($"Too few arguments for function {Name}. Expected {RequiredParameters}, Got {handle.Arguments.Count}");
            if (Arguments.Count < handle.Arguments.Count)
                throw new Exception($"Too many arguments for function {Name}. Expected {RequiredParameters}, got {handle.Arguments.Count}");
            var idx = 0;
            foreach (var arg in handle.Arguments)
            {
                if (!typeCheck(arg, Arguments[idx].Type))
                {
                    return false;
                }
                ++idx;
            }
            return true;
        }

    }
}

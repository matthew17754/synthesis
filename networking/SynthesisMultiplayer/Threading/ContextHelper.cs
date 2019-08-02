using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public class ContextHelper
    {
        public static dynamic[] GetContextData(ref ITaskContext context, params string[] keys)
        {
            List<dynamic> res = new List<dynamic>();
            foreach(var key in keys)
            {
                res.Add(context.GetObject(key));
            }
            return res.ToArray();
        }
    }
}

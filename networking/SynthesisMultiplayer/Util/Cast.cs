using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Util
{
    public static class DynamicCast
    {
        public static dynamic Cast(object obj, Type to) => typeof(DynamicCast)
                .GetMethod("CastImpl", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .MakeGenericMethod(to)
                .Invoke(obj, new[] { obj });
        static T CastImpl<T>(object obj) where T : class => obj as T;
    }
}

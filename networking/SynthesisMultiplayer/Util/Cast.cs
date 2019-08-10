using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Util
{
    public partial class Util
    {
        public static dynamic DynamicCast(object obj, Type to)
        {
            return typeof(Util).GetMethod("Cast", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(to)
                .Invoke(obj, new[] { obj });
        }
        static T Cast<T>(object obj) where T : class
        {
            return obj as T;
        }
    }
}

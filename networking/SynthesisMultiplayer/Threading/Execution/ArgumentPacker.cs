using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading.Execution
{
    public static class ArgumentPacker
    {
        private static void CheckCount(Queue<dynamic> q, int count)
        {
            if(q.Count < count)
            {
                throw new Exception("Not enough args");
            }
        }
        public static ValueTuple<T> Unpack<T>(Queue<dynamic> q)
        {
            CheckCount(q, 1);
            return (q.Dequeue());
        }
        public static ValueTuple<T1,T2> Unpack<T1,T2>(Queue<dynamic> q)
        {
            CheckCount(q, 2);
            return (q.Dequeue(), q.Dequeue());
        }
        public static ValueTuple<T1,T2,T3> Unpack<T1,T2,T3>(Queue<dynamic> q)
        {
            CheckCount(q, 3);
            return (q.Dequeue(), q.Dequeue(), q.Dequeue());
        }
        public static ValueTuple<T1,T2,T3,T4> Unpack<T1,T2,T3,T4>(Queue<dynamic> q)
        {
            CheckCount(q, 4);
            return (q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue());
        }
        public static ValueTuple<T1,T2,T3,T4,T5> Unpack<T1,T2,T3,T4,T5>(Queue<dynamic> q)
        {
            CheckCount(q, 5);
            return (q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue());
        }
        public static ValueTuple<T1,T2,T3,T4,T5,T6> Unpack<T1,T2,T3,T4,T5,T6>(Queue<dynamic> q)
        {
            CheckCount(q, 6);
            return (q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue());
        }
        public static ValueTuple<T> Unpack<T>(this AsyncCallHandle handle)
        {
            CheckCount(handle.Arguments, 1);
            return (handle.Arguments.Dequeue());
        }
        public static ValueTuple<T1,T2> Unpack<T1,T2>(this AsyncCallHandle handle)
        {
            CheckCount(handle.Arguments, 2);
            return (handle.Arguments.Dequeue(), handle.Arguments.Dequeue());
        }
        public static ValueTuple<T1,T2,T3> Unpack<T1,T2,T3>(this AsyncCallHandle handle)
        {
            CheckCount(handle.Arguments, 3);
            return (handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue());
        }
        public static ValueTuple<T1,T2,T3,T4> Unpack<T1,T2,T3,T4>(this AsyncCallHandle handle)
        {
            CheckCount(handle.Arguments, 4);
            return (handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue());
        }
        public static ValueTuple<T1,T2,T3,T4,T5> Unpack<T1,T2,T3,T4,T5>(this AsyncCallHandle handle)
        {
            CheckCount(handle.Arguments, 5);
            return (handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue());
        }
        public static ValueTuple<T1,T2,T3,T4,T5,T6> Unpack<T1,T2,T3,T4,T5,T6>(this AsyncCallHandle handle)
        {
            CheckCount(handle.Arguments, 6);
            return (handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue());
        }
    }
}

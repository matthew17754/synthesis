using Multiplayer.Attribute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Multiplayer.Threading.Runtime
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
        private static CallbackInfo GetMethodInfo()
        {
            // TODO: Find a way to do this that *doesn't* involve reading stack
            var frame = new StackFrame(2);
            var method = frame.GetMethod();
            var methodName = "";
            var hasMethod = false;
            var arguments = new List<ArgumentAttribute>();
            var argumentOrder = new List<string>();
            var returnType = new ReturnTypeAttribute(typeof(void));
            foreach(var attr in method.GetCustomAttributes(false))
            {
                if(attr is CallbackAttribute)
                {
                    hasMethod = true;
                    methodName = ((CallbackAttribute)attr).Name;
                    argumentOrder = ((CallbackAttribute)attr).ArgumentNames;
                }
                else if (attr is ArgumentAttribute)
                {
                    arguments.Add((ArgumentAttribute)attr);
                } else if (attr is ReturnTypeAttribute)
                {
                    returnType = (ReturnTypeAttribute)attr;
                }
            }
            if (!hasMethod)
                throw new Exception("Cannot call GetArgs from a method without attributes");
            arguments = argumentOrder
                .Join(arguments,
                    name => name,
                    arg => arg.Name,
                    (argName, arg) => arg
                ).ToList();
            return new CallbackInfo(methodName, arguments, returnType);
        }

        public static CallbackInfo GetMethodInfo(Type baseType, string methodName)
        {
            // TODO: Find a way to do this that *doesn't* involve reading stack
            var method = baseType.GetMethods()
                .Where(m => m.GetCustomAttribute(typeof(CallbackAttribute), false) != null)
                .Where(m => ((CallbackAttribute)m.GetCustomAttribute(typeof(CallbackAttribute), false)).Name == methodName)
                .First();
            if (method == null)
                throw new Exception($"No method exists named '{methodName}' in type '{baseType.Name}'");
            var hasMethod = false;
            var arguments = new List<ArgumentAttribute>();
            var argumentOrder = new List<string>();
            var returnType = new ReturnTypeAttribute(typeof(void));
            foreach(var attr in method.GetCustomAttributes(false))
            {
                if(attr is CallbackAttribute)
                {
                    hasMethod = true;
                    methodName = ((CallbackAttribute)attr).Name;
                    argumentOrder = ((CallbackAttribute)attr).ArgumentNames;
                }
                else if (attr is ArgumentAttribute)
                {
                    arguments.Add((ArgumentAttribute)attr);
                } else if (attr is ReturnTypeAttribute)
                {
                    returnType = (ReturnTypeAttribute)attr;
                }
            }
            if (!hasMethod)
                throw new Exception("Cannot call GetArgs from a method without attributes");
            arguments = argumentOrder
                .Join(arguments,
                    name => name,
                    arg => arg.Name,
                    (argName, arg) => arg
                ).ToList();
            return new CallbackInfo(methodName, arguments, returnType);
        }


        public static T Unpack<T>(Queue<dynamic> q)
        {
            CheckCount(q, 1);
            return q.Dequeue();
        }
        public static (T1,T2) Unpack<T1,T2>(Queue<dynamic> q)
        {
            CheckCount(q, 2);
            return (q.Dequeue(), q.Dequeue());
        }
        public static (T1,T2,T3) Unpack<T1,T2,T3>(Queue<dynamic> q)
        {
            CheckCount(q, 3);
            return (q.Dequeue(), q.Dequeue(), q.Dequeue());
        }
        public static (T1,T2,T3,T4) Unpack<T1,T2,T3,T4>(Queue<dynamic> q)
        {
            CheckCount(q, 4);
            return (q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue());
        }
        public static (T1,T2,T3,T4,T5) Unpack<T1,T2,T3,T4,T5>(Queue<dynamic> q)
        {
            CheckCount(q, 5);
            return (q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue());
        }
        public static (T1,T2,T3,T4,T5,T6) Unpack<T1,T2,T3,T4,T5,T6>(Queue<dynamic> q)
        {
            CheckCount(q, 6);
            return (q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue(), q.Dequeue());
        }

        public static T GetArgs<T>(AsyncCallHandle handle)
        {
            var methodInfo = GetMethodInfo();
            CheckCount(handle.Arguments, methodInfo.RequiredParameters);
            methodInfo.ValidateArguments(handle);
            return handle.Arguments.Dequeue();
        }
        public static (T1,T2) GetArgs<T1,T2>(AsyncCallHandle handle)
        {
            var methodInfo = GetMethodInfo();
            CheckCount(handle.Arguments, methodInfo.RequiredParameters);
            methodInfo.ValidateArguments(handle);
            return (handle.Arguments.Dequeue(), handle.Arguments.Dequeue());
        }
        public static (T1,T2,T3) GetArgs<T1,T2,T3>(AsyncCallHandle handle)
        {
            var methodInfo = GetMethodInfo();
            CheckCount(handle.Arguments, methodInfo.RequiredParameters);
            methodInfo.ValidateArguments(handle);
            return (handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue());
        }
        public static (T1,T2,T3,T4) GetArgs<T1,T2,T3,T4>(AsyncCallHandle handle)
        {
            var methodInfo = GetMethodInfo();
            CheckCount(handle.Arguments, methodInfo.RequiredParameters);
            methodInfo.ValidateArguments(handle);
            return (handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue());
        }
        public static (T1,T2,T3,T4,T5) GetArgs<T1,T2,T3,T4,T5>(AsyncCallHandle handle)
        {
            var methodInfo = GetMethodInfo();
            CheckCount(handle.Arguments, methodInfo.RequiredParameters);
            methodInfo.ValidateArguments(handle);
            return (handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue());
        }
        public static (T1,T2,T3,T4,T5,T6) GetArgs<T1,T2,T3,T4,T5,T6>(AsyncCallHandle handle)
        {
            var methodInfo = GetMethodInfo();
            CheckCount(handle.Arguments, methodInfo.RequiredParameters);
            methodInfo.ValidateArguments(handle);
            return (handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue(), handle.Arguments.Dequeue());
        }

    }
}

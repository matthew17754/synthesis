using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Util
{
    public class Optional<T>
    {
        T value;
        bool valid;

        public Optional()
        {
            valid = false;
        }

        public Optional(T v)
        {
            value = v;
            valid = true;
        }
        public bool IsValid()
        {
            return valid;
        }
        public void Set(T v)
        {
            value = v;
            valid = true;
        }
        public void Invalidate()
        {
            valid = false;
        }
        public static implicit operator T(Optional<T> t) => t.Get();
        public T Get()
        {
            if (valid) 
                return value;
            throw new Exception("Attempt to get value from invalid optional");
        }
        public static Optional<R> Map<R>(Func<T, R> f, Optional<T> t)
        {
            if (t.valid)
                return new Optional<R>(f(t));
            return new Optional<R>();
        }
        public static Func<Optional<T>, Optional<R>> Lift<R>(Func<T, R> f)
        {
            return (Optional<T> t) =>
            {
                if (t.valid)
                    return new Optional<R>(f(t));
                return new Optional<R>();
            };
        }
    }
}

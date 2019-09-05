using System;

namespace Multiplayer.Collections
{
    public class Optional<T> : IDisposable
    {
        bool disposedValue = false;
        T value;
        public bool Valid { get; private set; }

        public Optional()
        {
            Valid = false;
        }

        public Optional(T v)
        {
            value = v;
            Valid = true;
        }
        public void Set(T v)
        {
            value = v;
            Valid = true;
        }
        public void Invalidate()
        {
            Valid = false;
        }
        public static implicit operator T(Optional<T> t) => t.Get();
        public T Get()
        {
            if (Valid) 
                return value;
            throw new Exception("Attempt to get value from invalid optional");
        }
        public static Optional<R> Map<R>(Func<T, R> f, Optional<T> t)
        {
            if (t.Valid)
                return new Optional<R>(f(t));
            return new Optional<R>();
        }
        public static Func<Optional<T>, Optional<R>> Lift<R>(Func<T, R> f)
        {
            return (Optional<T> t) =>
            {
                if (t.Valid)
                    return new Optional<R>(f(t));
                return new Optional<R>();
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
    }
}

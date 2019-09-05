using Multiplayer.Collections;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Multiplayer.IPC
{
    public class Channel<T> : IDisposable
    {
        bool open;
        Mutex mutex;
        Queue<T> buffer;
        int waitTimeout;
        EventWaitHandle eventWaitHandle;
        bool disposedValue = false;
        public int Count { get => buffer.Count; }
        public Channel(int timeout = 5)
        {
            mutex = new Mutex();
            buffer = new Queue<T>();
            waitTimeout = timeout;
            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            open = true;
        }

        public void Send(T item)
        {
            if (!open)
                throw new Exception("Attempt to push to closed channel");
            lock(mutex)
            {
                buffer.Enqueue(item);
                eventWaitHandle.Set();
            }
        }
        public void Pop()
        {
            lock (mutex)
                buffer.Dequeue();
        }
        public T Get()
        {
            if(Count == 0)
                eventWaitHandle.WaitOne();
            lock (mutex)
                return buffer.Dequeue();
        }
        public Optional<T> TryGet()
        {
            if(eventWaitHandle.WaitOne(waitTimeout))
            {
                lock (mutex)
                    return Count != 0 ? new Optional<T>(buffer.Dequeue()) : new Optional<T>();
            }
            return new Optional<T>();
        }
        public T Peek()
        {
            eventWaitHandle.WaitOne();
            lock (mutex)
                return buffer.Peek();
        }
        public Optional<T> TryPeek()
        {
            lock (mutex)
                return Count != 0 ? new Optional<T>(buffer.Peek()) : new Optional<T>();

        }
        public void Close()
        {
            open = false;
        }
        public static (Channel<T>, Channel<T>) CreateMPSCChannel()
        {
            return (new Channel<T>(), new Channel<T>());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                    lock (mutex)
                        buffer.Clear();
                    mutex.Dispose();
                    eventWaitHandle.Dispose();
                    
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

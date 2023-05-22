using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Dragonfly.Utils
{
    /// <summary>
    /// A thread-safe queue collection that blocks readers if empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BlockingQueue<T>
    {
        private Queue<T> q;
        private Queue<T> qCache;

        public bool Released
        {
            get; private set;
        }

        public BlockingQueue()
        {
            q = new Queue<T>();
            qCache = new Queue<T>();
        }

        /// <summary>
        /// Lock this queue for the current thread, temporarely stopping other thead to access.
        /// Can be used to batch multiple operations on the queue, and prevent the scheduler wasting time in contexts with a lot of contention. Call Unlock when done.
        /// </summary>
        public void Lock()
        {
            Monitor.Enter(q);
        }

        public void Unlock()
        {
            Monitor.Exit(q);
        }

        public void Enqueue(T item)
        {
            lock (q)
            {
                q.Enqueue(item);
                Monitor.PulseAll(q);
            }
        }

        public int Count
        {
            get
            {
                return q.Count;
            }
        }

        /// <summary>
        /// Try removing the next queued item. If this queue is empty, the caller is blocked until an item is available.
        /// </summary>
        public bool TryDequeue(out T item)
        {
            return TryDequeue(out item, Timeout.Infinite);
        }

        public bool TryDequeue(out T item, int millisecondsTimeout)
        {
            lock (q)
            {
                while (q.Count == 0)
                {
                    if (Released)
                    {
                        item = default(T);
                        return false;
                    }

                    bool timeout = !Monitor.Wait(q, millisecondsTimeout);// release the lock
                    if (timeout)
                    {
                        item = default(T);
                        return false;
                    }
                }

                item = q.Dequeue();
                return true;
            }
        }

        public void Remove(T item)
        {
            lock (q)
            {
                if (q.Count == 0)
                    return;

                // move all elements except the one to remove to a temp list, then swap them.
                while (q.Count > 0)
                {
                    T elem = q.Dequeue();
                    if (!elem.Equals(item))
                        qCache.Enqueue(elem);
                }
                Queue<T> temp = q;
                q = qCache;
                qCache = temp;
            }
        }

        public void Clear()
        {
            lock (q)
            {
                q.Clear();
                Monitor.PulseAll(q);
            }
        }

        public void Release()
        {
            lock (q)
            {
                Released = true;
                Monitor.PulseAll(q);
            }
        }

    }
}

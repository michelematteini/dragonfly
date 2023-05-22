using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Dragonfly.Utils
{
    public static class SlimParallel
    {
        public interface ITaskBody
        {
            void Execute();
        }

        public interface IForBody
        {
            void Execute(int i);
        }

        enum PooledTaskType
        {
            ForChunk,
            SingleTask
        }

        struct TaskArgs
        {
            public PooledTaskType TaskType;

            // for task args
            public int From, To;
            public IForBody ForBody;
            public CountdownEvent ChunkCompletionEvent;

            // single task args
            public ITaskBody SingleTaskBody;
        }

        class PooledThread
        {
            public Thread Thread;
        }

        private static List<PooledThread> threadPool;
        private static BlockingQueue<TaskArgs> taskQueue;
        private static ThreadLocal<CountdownEvent> syncEvent;

        static SlimParallel()
        {
            syncEvent = new ThreadLocal<CountdownEvent>(() => new CountdownEvent(1), false);
            taskQueue = new BlockingQueue<TaskArgs>();

            threadPool = new List<PooledThread>(Environment.ProcessorCount + 1);
            for (int i = 0; i <= Environment.ProcessorCount; i++)
            {
                PooledThread t = new PooledThread();
                t.Thread = new Thread(new ThreadStart(PooledThreadLoop));
                t.Thread.IsBackground = true;
                t.Thread.Name = "PooledThread" + i;
                t.Thread.Start();
                threadPool.Add(t);
            }
        }

        private static void PooledThreadLoop()
        {
            TaskArgs task;

            while (true)
            {
                // dequeue a new task
                if (!taskQueue.TryDequeue(out task))
                    continue;

                // perform the required task
                switch (task.TaskType)
                {
                    case PooledTaskType.ForChunk:
                        {
                            for (int i = task.From; i < task.To; i++)
                                task.ForBody.Execute(i);
                            task.ChunkCompletionEvent.Signal();
                        }
                        break;

                    case PooledTaskType.SingleTask:
                        {
                            task.SingleTaskBody.Execute();
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public static void For(int fromInclusive, int toExclusive, int minChunkSize, IForBody body)
        {
            int iterCount = toExclusive - fromInclusive;
            int chunkSize = Math.Max(minChunkSize, 1 + iterCount / Math.Max(20, threadPool.Count));

            // execute the for loop synchronously if a single chunk should be executed
            if (chunkSize >= iterCount)
            {
                for (int i = fromInclusive; i < toExclusive; i++)
                    body.Execute(i);
                
                return;
            }

            CountdownEvent curSyncEvent = syncEvent.Value;
            int chunkCount = (iterCount + chunkSize - 1) / chunkSize;
            curSyncEvent.Reset(chunkCount + 1);
            taskQueue.Lock();
            for (int iFrom = fromInclusive, iChunk = 0; iFrom < toExclusive; iFrom += chunkSize, iChunk++)
            {
                // prepare chunk
                TaskArgs chunk = new TaskArgs() { TaskType = PooledTaskType.ForChunk };
                chunk.From = iFrom;
                chunk.To = Math.Min(iFrom + chunkSize, toExclusive);
                chunk.ChunkCompletionEvent = curSyncEvent;
                chunk.ForBody = body;

                // queue chunk execution
                taskQueue.Enqueue(chunk);
            }
            taskQueue.Unlock();
            curSyncEvent.Signal();
            curSyncEvent.Wait();
        }

        public static void RunAsync(ITaskBody body)
        {
            TaskArgs singleTask = new TaskArgs() { TaskType = PooledTaskType.SingleTask };
            singleTask.SingleTaskBody = body;
            taskQueue.Enqueue(singleTask);
        }

    }
}

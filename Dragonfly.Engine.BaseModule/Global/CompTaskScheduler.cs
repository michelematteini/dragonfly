using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Helper component that manage engine-synchronous user tasks returning a value indicating if it should be run or not.
    /// The ammount of work per frame is automatically adjusted to accomodate for the ammount of tasks.
    /// </summary>
    public class CompTaskScheduler : Component, ICompUpdatable
    {
        private IndexedList<Task> scheduledTasks;
        private int skippedTaskCount; // number of task not processed in the last update
        private int prevSkippedTaskCount; // number of task not processed before the last update
        private int maxTasksPerFrame; // number of tasks that can at most be executed each frame

        internal CompTaskScheduler(Component parent) : base(parent)
        {
            scheduledTasks = new IndexedList<Task>();
            maxTasksPerFrame = 1;
        }

        public ITask CreateTask(string taskName, Action body, int intervalInFrames = 0)
        {
            return new Task(this) { Name = taskName, Body = body, IntervalInFrames = intervalInFrames };
        }

        public ITask CreateTask(string taskName, Action body, float intervalSeconds)
        {
            return new Task(this) { Name = taskName, Body = body, IntervalInFrames = -1, IntervalInSeconds = intervalSeconds };
        }

        public UpdateType NeededUpdates
        {
            get
            {
                return scheduledTasks.Count > 0 ? UpdateType.FrameStart2 : UpdateType.None;
            }
        }

        public int LastFrameTaskCount { get; private set; }

        public void Update(UpdateType updateType)
        {
            // predict a maxTasksPerFrame that will better distribute the tasks
            if ((skippedTaskCount - prevSkippedTaskCount) > 0)
                maxTasksPerFrame++;
            else if (skippedTaskCount == 0 && maxTasksPerFrame > 1)
                maxTasksPerFrame--;

            // reset task counters
            LastFrameTaskCount = 0;
            prevSkippedTaskCount = skippedTaskCount;
            skippedTaskCount = 0;

            // process al tasks
            for (int i = 0; i < scheduledTasks.Size; i++)
            {
                Task t = scheduledTasks[i];

                if (t == scheduledTasks.EmptyValue)
                    continue;

                if (t.State == TaskState.WaitingExecution)
                {
                    if (IsIntervalExpired(t))
                    {
                        if (LastFrameTaskCount >= maxTasksPerFrame)
                        {
                            // track pressure on the schedule without executing, since we are out of frame budget
                            skippedTaskCount++;
                        }
                        else
                        {
                            t.State = TaskState.Executing;
                            t.LastExecutionFrame = Context.Time.FrameIndex;
                            t.LastExecutionTime = Context.Time.RealSecondsFromStart;
                            SlimParallel.RunAsync(t);
                            LastFrameTaskCount++;
                        }
                    }
                }
                        
            }
        }

        /// <summary>
        /// Check if the task waiting interval is expired and should be executed.
        /// </summary>
        private bool IsIntervalExpired(Task t)
        {
            if (t.IntervalInFrames < 0)
            {
                if ((Context.Time.RealSecondsFromStart - t.LastExecutionTime).FloatValue < t.IntervalInSeconds)
                    return false; // seconds interval still not elapsed
            }
            else
            {
                if ((Context.Time.FrameIndex - t.LastExecutionFrame) < t.IntervalInFrames)
                    return false; // frames interval still not elapsed
            }

            return true;
        }

        private void QueueExecution(Task t)
        {
            lock (t.STATE_LOCK)
            {
                if (t.State != TaskState.Idle)
                    return;  // invalid state or already queued
                t.SchedulerID = scheduledTasks.Add(t); // add to the scheduler if idle
                t.State = TaskState.WaitingExecution;
            }
        }

        private void Reset(Task t)
        {
            lock (t.STATE_LOCK)
            {
                if (t.State == TaskState.Idle)
                    return;
#if DEBUG
                if (t.State != TaskState.Completed)
                    throw new InvalidOperationException("Reset can only be called on a task that is not currently executing");
#endif

                scheduledTasks.RemoveAt(t.SchedulerID);
                t.State = TaskState.Idle;
            }
        }

        public enum TaskState
        {
            Idle,
            WaitingExecution,
            Executing,
            Completed
        }

        public interface ITask
        {
            string Name { get; }

            TaskState State { get; }

            /// <summary>
            /// Queue this task for execution. If this task is already queued or executing, it will not be queued a second time.
            /// If the task is in a Completed state, this call will throw an exception.
            /// </summary>
            void QueueExecution();

            /// <summary>
            /// Resets a completed task so that it can be executed again.
            /// </summary>
            void Reset();
        }

        private class Task : ITask, SlimParallel.ITaskBody
        {
            internal object STATE_LOCK;

            private CompTaskScheduler scheduler;
            public Action Body;
            public int IntervalInFrames;
            public float IntervalInSeconds;
            public int LastExecutionFrame;
            public PreciseFloat LastExecutionTime;
            public int SchedulerID;

            public Task(CompTaskScheduler scheduler)
            {
                this.scheduler = scheduler;
                STATE_LOCK = new object();
            }
            
            public TaskState State { get; set; }

            public string Name { get; set; }

            public void Execute()
            {
                Body();
                lock (STATE_LOCK)
                {
                    State = TaskState.Completed;
                }
            }

            public void Reset()
            {
                scheduler.Reset(this);
            }

            public void QueueExecution()
            {
                scheduler.QueueExecution(this);
            }
        }

    }

}

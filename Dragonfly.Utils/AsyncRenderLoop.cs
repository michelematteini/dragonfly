using System;
using System.Threading;

namespace Dragonfly.Utils
{
    /// <summary>
    /// An engine render loop that run on a separate thread. Allow for asynchronous stop requests and framerate control.
    /// </summary>
    public class AsyncRenderLoop : IRenderLoop
    {
        private RenderLoopEventArgs renderArgs;
        private ResumeLoopEventArgs resumeArgs;
        private bool stopRequest;
        private Thread renderingThread;
        private ManualResetEvent notPausedEvent;

        public AsyncRenderLoop()
        {
            renderArgs = new RenderLoopEventArgs();
            resumeArgs = new ResumeLoopEventArgs();
            notPausedEvent = new ManualResetEvent(true);
            MaxFramesPerSecond = 60;
        }

        public event RenderLoopEventHandler FrameRequest;

        public event ResumeLoopEventHandler ResumeAttempt;

        public int MaxFramesPerSecond { get; set; }

        public bool IsRunning { get { return IsAlive && !IsPaused; } }

        public bool IsPaused { get { return !notPausedEvent.WaitOne(0); } }

        public bool IsAlive { get { return renderingThread != null && renderingThread.IsAlive; } }

        public void Play()
        {
            stopRequest = false;

            resume();

            if (!IsAlive)
            {
                renderingThread = new Thread(new ThreadStart(renderingLoop));
                renderingThread.Start();
            }
        }

        public void Stop()
        {
            stopRequest = true;
            resume();          
        }

        private void resume()
        {
            if (IsPaused) notPausedEvent.Set();
        }

        public void Pause()
        {
            notPausedEvent.Reset();
        }

        public void StopNow()
        {
            Stop();
            renderingThread.Join();
        }

        private void renderingLoop()
        {
            while(!stopRequest)
            {
                // check if used requested a resume loop for rendering failure
                if (renderArgs.TryResume)
                {
                    ResumeAttempt(resumeArgs);

                    if(!resumeArgs.ResumeSucceeded)
                    {
                        // wait and try again later!
                        Thread.Sleep(1000);
                        continue;
                    }

                    // resume succeeded, reset resume states and continue rendering
                    renderArgs.TryResume = false;
                    resumeArgs.ResumeSucceeded = false;
                }

                // render a frame and sleep-pad to achieve the required framerate
                DateTime beforeTime = DateTime.Now;
                FrameRequest(renderArgs);
                DateTime afterTime = DateTime.Now;
                double elapsedMs = (afterTime - beforeTime).TotalMilliseconds;
                double waitMs = (1000.0 / MaxFramesPerSecond) - elapsedMs;
                Thread.Sleep(waitMs > 0 ? (int)waitMs : 0);

                notPausedEvent.WaitOne(); // wait here when paused
            }
        }

        
    }
}

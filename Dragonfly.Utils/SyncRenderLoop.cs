using System;

namespace Dragonfly.Utils
{
    /// <summary>
    /// A render loop that run synchronously and block the caller on Play().
    /// </summary>
    public class SyncRenderLoop : IRenderLoop
    {
        private bool stopRequest;
        private RenderLoopEventArgs renderArgs;
        private ResumeLoopEventArgs resumeArgs;

        public SyncRenderLoop()
        {
            renderArgs = new RenderLoopEventArgs();
            resumeArgs = new ResumeLoopEventArgs();
        }

        public bool IsRunning { get; private set; }

        public event RenderLoopEventHandler FrameRequest;
        public event ResumeLoopEventHandler ResumeAttempt;

        public void Pause()
        {
            stopRequest = true;
        }

        public void Play()
        {
            stopRequest = false;
            IsRunning = true;

            while (!stopRequest)
            {
                // check if used requested a resume loop for rendering failure
                if (renderArgs.TryResume)
                {
                    ResumeAttempt(resumeArgs);

                    if (!resumeArgs.ResumeSucceeded)
                        continue; // try again

                    // resume succeeded, reset resume states and continue rendering
                    renderArgs.TryResume = false;
                    resumeArgs.ResumeSucceeded = false;
                }

                // render a frame and sleep-pad to achieve the required framerate
                FrameRequest(renderArgs);
            }
        }

        public void Stop()
        {
            stopRequest = true;
        }
    }
}

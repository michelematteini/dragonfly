using System;
using System.Threading;

namespace Dragonfly.Utils
{
    public class WindowRenderLoop : IRenderLoop
    {
        public interface IWindow
        {
            void SetCallbackOnIdle(Action callback);

            void ResetCallbackOnIdle();

            bool IsIdle { get; }
        }

        private Timer resumeTimer;
        private RenderLoopEventArgs renderArgs;
        private ResumeLoopEventArgs resumeArgs;
        private IWindow parentWindow;

        public WindowRenderLoop(IWindow parentWindow)
        {
            this.parentWindow = parentWindow;
            renderArgs = new RenderLoopEventArgs();
            resumeArgs = new ResumeLoopEventArgs();
            resumeTimer = new Timer(new TimerCallback(TryResume), null, Timeout.Infinite, 0);
        }

        public event RenderLoopEventHandler FrameRequest;

        public event ResumeLoopEventHandler ResumeAttempt;

        public bool IsRunning { get; private set; }

        public void Play()
        {
            parentWindow.SetCallbackOnIdle(RenderLoop);
            IsRunning = true;
        }

        public void Pause()
        {
            Stop();
        }

        public void Stop()
        {
            resumeTimer.Change(Timeout.Infinite, 0);
            parentWindow.ResetCallbackOnIdle();
            IsRunning = false;
        }

        private void TryResume(object state)
        {
            if (IsRunning) 
                return;

            resumeArgs.ResumeSucceeded = false;
            ResumeAttempt(resumeArgs);
            if(resumeArgs.ResumeSucceeded)
            {
                resumeTimer.Change(Timeout.Infinite, 0);
                Play();
            }
        }

        private void RenderLoop()
        {
            if (!IsRunning)
                return;

            renderArgs.TryResume = false;
            while (parentWindow.IsIdle)
            {
                FrameRequest(renderArgs);
                if(renderArgs.TryResume)
                {
                    parentWindow.ResetCallbackOnIdle();
                    resumeTimer.Change(0, 1000);
                    IsRunning = false;
                    return;
                }
            }
        }
    }

  

}

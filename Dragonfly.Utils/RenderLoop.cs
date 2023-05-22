using System;

namespace Dragonfly.Utils
{

    public interface IRenderLoop
    {
        event RenderLoopEventHandler FrameRequest;

        event ResumeLoopEventHandler ResumeAttempt;

        void Play();

        void Stop();

        void Pause();

        bool IsRunning { get; }
    }

    public delegate void RenderLoopEventHandler(RenderLoopEventArgs e);

    public class RenderLoopEventArgs : EventArgs
    {
        public bool TryResume { get; set; }
    }

    public delegate void ResumeLoopEventHandler(ResumeLoopEventArgs e);

    public class ResumeLoopEventArgs : EventArgs
    {
        public bool ResumeSucceeded { get; set; }
    }

}
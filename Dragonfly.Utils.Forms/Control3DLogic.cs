using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using System;


namespace Dragonfly.Utils.Forms
{
    internal static class Control3DLogic
    {
        public static EngineContext CreateDefaultEngine(IControl3D owner)
        {
            EngineParams ep = new EngineParams();
            ep.StartTime = DateTime.Now;
            ep.Target = owner.GetTargetControl();
            ep.AntiAliasing = owner.Antialising;
            ep.ResourceFolder = string.IsNullOrEmpty(owner.StartupPath) ? PathEx.DefaultResourceFolder : owner.StartupPath;

            EngineContext engine = EngineFactory.CreateContext(ep);
            engine.AddModule(new BaseMod());
            return engine;
        }

        public static IRenderLoop CreateRenderLoop(IControl3D owner, Action<Exception> errorCallback)
        {
            IRenderLoop renderLoop;

            if (owner.RenderOnMainThread)
                renderLoop = new WindowRenderLoop(new Control3DLoopWindow());
            else
                renderLoop = new AsyncRenderLoop();

            if (owner.CaptureErrors)
                renderLoop.FrameRequest += e => safeFrameRequest(renderLoop, owner, errorCallback, e);
            else
                renderLoop.FrameRequest += e => e.TryResume = !owner.Engine.RenderFrame();

            renderLoop.ResumeAttempt += (ResumeLoopEventArgs e) => 
            {
                e.ResumeSucceeded = owner.Engine.CanRender;
            };

            return renderLoop;
        }

        private static void safeFrameRequest(IRenderLoop renderLoop, IControl3D owner, Action<Exception> errorCallback, RenderLoopEventArgs e)
        {
            try
            {
                e.TryResume = !owner.Engine.RenderFrame();
            }
            catch (Exception ex)
            {
                renderLoop.Stop();
                errorCallback?.Invoke(ex);
            }
        }

    }
}

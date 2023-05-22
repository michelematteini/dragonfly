using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using System;
using System.Drawing;
using System.Threading;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Helper class that manage screenshot rendering during an on-screen rendering loop.
    /// <para/> Manages rendering to textures at a resolution different from the one currently active without affecting on-screen rendering.
    /// </summary>
    public class CompScreenshot : Component, ICompUpdatable
    {
        private enum State
        {
            Idle = 0,
            Requested,
            ChangingResolution,
            FramePrepared,
            WaitingRender
        }

        private static AutoResetEvent pipelineAccessLock = new AutoResetEvent(true);
        private CompRenderBuffer screenshotBuffer;
        private Action<Bitmap> onScreeshotReadyCallback;
        private State state;

        // current pipeline state
        private bool wasRenderingToTexture;
        private CompRenderBuffer currentRenderBuffer;
        private ResizeStyle curResizeStyle;
        private Int2 curResolution;
         

        public CompScreenshot(Component parent) : base(parent)        { }

        public void TakeScreenshot(Action<Bitmap> onScreeshotReadyCallback)
        {
            TakeScreenshot(onScreeshotReadyCallback, Context.Scene.Resolution);
        }

        public void TakeScreenshot(Action<Bitmap> onScreeshotReadyCallback, Int2 resolution)
        {
            if (state != State.Idle) return;
            screenshotBuffer = new CompRenderBuffer(this, SurfaceFormat.Color, resolution.X, resolution.Y);
            state = State.Requested;
            this.onScreeshotReadyCallback = onScreeshotReadyCallback;
        }

        public UpdateType NeededUpdates { get { return (state != State.Idle) ? UpdateType.FrameStart1 : UpdateType.None; } }

        public void Update(UpdateType updateType)
        {
            switch (state)
            {
                case State.Requested:
                    if (screenshotBuffer.LoadingRequired)
                        return; // waiting for the screenshot buffer to be ready
                    
                    if (!pipelineAccessLock.WaitOne(0))
                        return; // currently taking another screenshot;

                    state = State.ChangingResolution;

                    if (Context.Scene.Resolution == screenshotBuffer.Resolution)
                        goto case State.ChangingResolution;

                    // set global resolution to the needed one temporarely
                    curResizeStyle = Context.Scene.ResizeStyle;
                    curResolution = Context.Scene.Resolution;
                    Context.Scene.ResizeStyle = ResizeStyle.KeepResolution;
                    Context.Scene.Resolution = screenshotBuffer.Resolution;
                    break;

                case State.ChangingResolution:
                    // save current pipeline state
                    wasRenderingToTexture = Context.Scene.MainRenderPass.RenderToTexture;
                    currentRenderBuffer = Context.Scene.MainRenderPass.RenderBuffer;

                    // replace render target with this screenshot buffer (TODO: all rendering to screen should be replaced)
                    Context.Scene.MainRenderPass.RenderBuffer = screenshotBuffer;
                    Context.Scene.MainRenderPass.RenderToTexture = true;

                    state = State.FramePrepared;
                    break;

                case State.FramePrepared:
                    // restore previous pipeline state
                    Context.Scene.MainRenderPass.RenderBuffer = currentRenderBuffer;
                    Context.Scene.MainRenderPass.RenderToTexture = wasRenderingToTexture;

                    // restore previous resolution (will be applied on the next frame, not on this one)
                    Context.Scene.Resolution = curResolution;
                    Context.Scene.ResizeStyle = curResizeStyle;

                    // request a snapshot of the rt data
                    screenshotBuffer[0].SaveSnapshot();

                    // skip this frame to let resolution return to the previous value (would flicker otherwise)
                    Context.Scene.RenderingEnabled = false;

                    state = State.WaitingRender;
                    break;

                case State.WaitingRender:

                    // restore rendering
                    Context.Scene.RenderingEnabled = true;

                    // try recovering screenshot data
                    Bitmap screenshot = null;
                    if(screenshotBuffer[0].TryGetSnapshotAsBitmap(out screenshot))
                    {
                        onScreeshotReadyCallback(screenshot);
                        pipelineAccessLock.Set();
                        state = State.Idle;
                    }
                    break;
            }
        }
    }
}

using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A component that manage a baking process that queue a pass to the current scene to render a specific content to a render buffer.
    /// </summary>
    public class CompBaker : Component, ICompUpdatable
    {
        private enum State
        {
            Idle = 0,
            Rendering,
            Completed
        }

        private int lastBakeTriggeredCount;
        private State bakingState;

        public CompBaker(Component parent, CompRenderPass finalPass, Action<RenderTargetRef[]> onCompletion, CompEvent bakeStartEvent) : base(parent)
        {
            OnCompletion = onCompletion;
            FinalPass = finalPass;
            BakeStartEvent = bakeStartEvent;
            bakingState = State.Idle;
            BakeOnlyOnce = true;
            DisposeOnCompletion = true;
            BakeStartEventIsTrigger = true;
            FinalPass.DebugColor = Color.Yellow;
        }

        /// <summary>
        ///  If set to true, the baking process is only carried out once and then stop. If set is false, another baking process is started on the same target if triggered again by the starting event. 
        /// </summary>
        public bool BakeOnlyOnce { get; set; }

        /// <summary>
        /// After the baking is completed, if this value is true (default) this component destroy itself.
        /// </summary>
        public bool DisposeOnCompletion { get; set; }

        /// <summary>
        /// Each time this event occurs, a baking request is queued, or started immediately if no baking is already in progress.
        /// </summary>
        public CompEvent BakeStartEvent { get; private set; }

        /// <summary>
        /// If true, this component will bake if BakeStartEvent occurred again from the last bake, reagrdless of whether it's currently occurring.
        /// </summary>
        public bool BakeStartEventIsTrigger { get; set; }

        /// <summary>
        /// Called once the baking is completed has been successfully rendered to.
        /// </summary>
        public Action<RenderTargetRef[]> OnCompletion { get; set; }

        /// <summary>
        /// The final screen-space rendering pass used by this baker
        /// </summary>
        public CompRenderPass FinalPass { get; private set; }

        /// <summary>
        /// New baking operations won't start while this flag is set to true. 
        /// </summary>
        public bool Paused { get; set; }

        private bool BakeRequested
        {
            get
            {
                if (Paused)
                    return false;

                if(BakeStartEventIsTrigger)
                    return BakeStartEvent.TriggeredCount > lastBakeTriggeredCount;

                return BakeStartEvent.GetValue();
            }
        }

        public UpdateType NeededUpdates
        {
            get
            {
                if (BakeRequested && bakingState == State.Idle || bakingState == State.Rendering)
                    return UpdateType.FrameStart1;

                return UpdateType.None;
            }
        }
        public void Update(UpdateType updateType)
        {
            if (bakingState == State.Idle)
            {
                // start a new baking process
                InitializeBake();
            }
            else
            {
                // the pass has been rendered the previous frame, so now it's ready!

                // remove the baking pass from the pipeline
                Context.Scene.MainRenderPass.RequiredPasses.Remove(FinalPass);

                // invoke completion event
                if (OnCompletion != null)
                {
                    RenderTargetRef[] targets = new RenderTargetRef[FinalPass.RenderBuffer.SurfaceCount];
                    for (int i = 0; i < targets.Length; i++)
                        targets[i] = GetTarget(i);
                    OnCompletion(targets);
                }

                if (DisposeOnCompletion)
                    Dispose(); // destroy current component
                else if (BakeOnlyOnce)
                    bakingState = State.Completed; // stop component in this state
                else if (BakeRequested)
                    InitializeBake(); // start baking again immediately
                else
                    bakingState = State.Idle; // wait next trigger
            }
        }

        public RenderTargetRef GetTarget(int index = 0)
        {
            return new RenderTargetRef(FinalPass.RenderBuffer, index);
        }

        private void InitializeBake()
        {
            // link the baking render pass to the next frame
            Context.Scene.MainRenderPass.RequiredPasses.Add(FinalPass);
            bakingState = State.Rendering;
            lastBakeTriggeredCount = BakeStartEvent.TriggeredCount;
        }

    }
    
}

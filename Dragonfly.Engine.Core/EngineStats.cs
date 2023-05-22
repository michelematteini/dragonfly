using System;

namespace Dragonfly.Engine.Core
{
    public class EngineStats
    {
        private EngineContext context;

        internal EngineStats(EngineContext parentContext)
        {
            this.context = parentContext;
        }

        /// <summary>
        /// Returns the total number of components added to this scene
        /// </summary>
        public int ComponentCount { get { return context.Scene.Components.Count; } }

        /// <summary>
        /// Returns the total number of drawable components added to this scene
        /// </summary>
        public int DrawableCount { get { return context.Scene.Components.GetCount<CompDrawable>(); } }

        /// <summary>
        /// Returns the cumulative stats from all the passes executed in the last frame.
        /// </summary>
        public RenderStats LastFrame { get { return context.Scene.LastFrameStats; } }

    }
}

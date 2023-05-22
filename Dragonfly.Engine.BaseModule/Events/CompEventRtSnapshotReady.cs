using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Monitor a render target, signaling when a required snapshot is ready.
    /// </summary>
    public class CompEventRtSnapshotReady : Component, ICompUpdatable
    {
        private RenderTarget monitoredRt;
        private bool isSnapshotReady;

        public CompEventRtSnapshotReady(Component owner, RenderTarget monitoredRt) : base(owner)
        {
            this.monitoredRt = monitoredRt;
            Event = new CompEvent(this, IsOccurring, EventTriggerType.Occurred);
        }

        public CompEvent Event { get; private set; }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        public void Update(UpdateType updateType)
        {
            // the actual rt state must be cached here since NeededUpdates could be called from other threads
            isSnapshotReady = monitoredRt.IsSnapshotReady();
        }

        private bool IsOccurring()
        {
            return isSnapshotReady;
        }
    }
}

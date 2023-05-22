using Dragonfly.Engine.Core;
using Dragonfly.Utils;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Preserve the value history of a given component, up to a specified number of past frames.
    /// </summary>
    public class CompValueHistory<T> : Component, ICompUpdatable
    {
        private Component<T> monitoredValue;
        private CircularArray<T> history;

        public CompValueHistory(Component parent, Component<T> monitoredValue, int maxFrameCount) : base(parent)
        {
            this.monitoredValue = monitoredValue;
            this.history = new CircularArray<T>(maxFrameCount - 1);
        }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        public void Update(UpdateType updateType)
        {
            history.Shift(-1);
            history[0] = monitoredValue.GetValue();
        }

        /// <summary>
        /// Returns a value that the monitored component had in past frames.
        /// </summary>
        /// <param name="frameDelta">Specify how many frames to look back: e.g. passing 0 returns the current value, while 1 returns the previous frame value and so on.</param>
        public T GetValueAtFrame(int frameDelta)
        {
            return history[frameDelta];
        }
    }
}

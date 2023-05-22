using Dragonfly.Utils;

namespace Dragonfly.Engine.Core.IO
{
    public abstract class InputDevice
    {
        public abstract bool IsKeyDown(VKey key);

        public abstract void NewFrame();

        /// <summary>
        /// Free any resource and hook associated with this device. 
        /// After this call, this can no longer be used.
        /// </summary>
        public abstract void Unplug();
    }
}

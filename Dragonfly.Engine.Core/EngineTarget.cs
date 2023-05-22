using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Drawing;

namespace Dragonfly.Engine.Core
{
    public abstract class EngineTarget
    {
        public abstract int Width { get; }

        public abstract int Height { get; }

        public abstract event Action Resized;

        public abstract event Action<Point> DragOver;

        public abstract event Action<VKey> KeyDown;

        public abstract event Action<VKey> KeyUp;

        public abstract event Action Activate;

        public abstract event Action<Int2> CursorMove;

        public abstract event Action<int> MouseWheelRotated;

        public EngineContext CurrentEngine { get; internal set; }

        public abstract bool IsNativeWindow { get; }

        public abstract IntPtr NativeHandle { get; }

        public abstract EngineTargetMode TargetMode { get; set; }
    }

    public enum EngineTargetMode
    {
        Windowed,
        Fullscreen
    }
}

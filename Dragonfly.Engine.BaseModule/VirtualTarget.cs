using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Drawing;

namespace Dragonfly.BaseModule
{
    public class VirtualTarget : EngineTarget
    {
        private int width, height;

        public VirtualTarget(int initialWidth, int initialHeight)
        {
            width = initialWidth;
            height = initialHeight;
            DragOver = (p) => { };
        }

        public override int Width { get { return width; } }

        public override int Height { get { return height; } }

        public override bool IsNativeWindow { get { return false; } }

        public override IntPtr NativeHandle { get { return IntPtr.Zero; } }

        public override EngineTargetMode TargetMode { get; set; }

        public override event Action Resized;

        public override event Action<Point> DragOver;

        public override event Action<VKey> KeyDown;

        public override event Action<VKey> KeyUp;

        public override event Action Activate;

        public override event Action<Int2> CursorMove;

        public override event Action<int> MouseWheelRotated;

        public void SetResolution(int width, int height)
        {
            this.width = width;
            this.height = height;
            Resized();
        }

        public void SetCursorPosition(int x, int y)
        {
            CursorMove(new Int2(x, y));
        }

        public void Pause()
        {

        }

        public void Resume()
        {
            Activate();
        }

        public void SendKeyDown(VKey key)
        {
            KeyDown(key);
        }

        public void SendKeyUp(VKey key)
        {
            KeyUp(key);
        }

        public void SendString(string s)
        {
            foreach(char c in s)
            {
                VKey key = c.ToVKey((VKey)0);
                if (key == 0) continue; // invalid, don't send
                KeyDown(key);
                KeyUp(key);
            }
        }

        public Bitmap ReadImage()
        {
            return CurrentEngine.Scene.MainRenderPass.RenderBuffer[0].ToBitmap();
        }

    }
}

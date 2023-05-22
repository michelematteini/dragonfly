using Dragonfly.Utils;
using Dragonfly.Engine.Core.IO;
using Dragonfly.Graphics.Math;
using System.Drawing;
using Dragonfly.Engine.Core;
using System;

namespace Dragonfly.BaseModule
{
	public class Mouse : InputDevice
	{
        private Int2 lastPos; // last cursor position appeared in the current frame events
        private Int2 prevFramePos; // last cursor potion appeared in the previous frame events
        private bool firstEvent; // true if this mouse input instance still hasn't received any cursor movement event.
		private Int2 frameDeltaPos; // current frame mouse offset
		private Int2 prevFrameDeltaPos; // last frame mouse offset
        private int mouseWheelDelta; // delta of the mouse wheel for the current frame
        private int prevMouseWheelDelta; // delta of the mouse wheel in the last frame
		
        
        private Keyboard keyboard;
        private EngineTarget inputSrc;
		
		public Mouse(EngineTarget inputSource, Keyboard keyboard)
		{
			firstEvent = true;
            this.keyboard = keyboard;
            inputSrc = inputSource;
            inputSrc.DragOver += InputSource_DragOver;
            inputSrc.CursorMove += OnCursorMove;
            inputSrc.MouseWheelRotated += OnMouseWheelRotated;

        }

        private void OnMouseWheelRotated(int delta)
        {
            mouseWheelDelta += delta;
        }

        public override void Unplug()
		{
            inputSrc.DragOver -= InputSource_DragOver;
            inputSrc.CursorMove -= OnCursorMove;
        }

		public override void NewFrame()
		{
            // reset/upate per frame mouse derivatives
            prevFrameDeltaPos = frameDeltaPos;
            frameDeltaPos = Int2.Zero;
            prevFramePos = lastPos;
            prevMouseWheelDelta = mouseWheelDelta;
            mouseWheelDelta = 0;
        }
		
		public bool IsLeftButtonPressed
		{
			get { return keyboard.IsKeyDown(VKey.VK_LBUTTON); }
		}
		
		public bool IsRightButtonPressed
		{
			get { return keyboard.IsKeyDown(VKey.VK_RBUTTON); }
		}
		
		public bool IsWheelPressed
		{
			get { return keyboard.IsKeyDown(VKey.VK_MBUTTON); }
		}
		
		public bool LeftClicked
		{
			get { return keyboard.KeyClicked(VKey.VK_LBUTTON); }
		}
		
		public bool RightClicked
		{
			get { return keyboard.KeyClicked(VKey.VK_RBUTTON); }
		}
		
		public bool WheelClicked
		{
			get { return keyboard.KeyClicked(VKey.VK_MBUTTON); }
		}
		
		public int ClickCount
		{
            get { return keyboard.GetKeyClickCount(VKey.VK_LBUTTON); }
		}

        public UiCoords Position
        {
            get
            {
                return new UiCoords(prevFramePos / new Float2(inputSrc.Width, inputSrc.Height), UiUnit.Percent);
            }
        }

        public UiSize DeltaPosition
        {
            get
            {
                return new UiSize(prevFrameDeltaPos / new Float2(inputSrc.Width, inputSrc.Height), UiUnit.Percent);
            }
        }

        public int WheelDelta
        {
            get
            {
                return prevMouseWheelDelta;
            }
        }

        private void InputSource_DragOver(Point mousePos)
        {
            OnCursorMove(new Int2(mousePos.X, mousePos.Y));
        }

        private void OnCursorMove(Int2 cursorPos)
        {
            // update mouse position derivative
            if (!firstEvent)
                frameDeltaPos += cursorPos - lastPos;

            lastPos = cursorPos;
            firstEvent = false;
        }

        public override bool IsKeyDown(VKey key)
        {
            switch(key)
            {
                case VKey.VK_LBUTTON:
                case VKey.VK_RBUTTON:
                case VKey.VK_MBUTTON:
                    return keyboard.IsKeyDown(key);
            }

            return false;
        }

    }
		
	
	
}



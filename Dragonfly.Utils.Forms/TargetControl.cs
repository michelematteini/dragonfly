using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using DragonflyUtils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Dragonfly.Utils.Forms
{
    public class TargetControl : EngineTarget
    {
        private Control control;
        private WindowResizeEndAdapted resizeAdapter;
        private WindowInputListener winInput, fullScreenWinInput;
        private EngineTargetMode currentMode;
        private Form fullScreenWindow;

        public TargetControl(Control control)
        {
            this.control = control;
            resizeAdapter = new WindowResizeEndAdapted(this.control);
            resizeAdapter.ResizeEnd += ResizeAdapter_ResizeEnd;
            control.DragOver += Control_DragOver;
            winInput = new WindowInputListener(control.Handle, OnWinMessage);
            currentMode = EngineTargetMode.Windowed;
        }

        private void Control_DragOver(object sender, DragEventArgs e)
        {
            Point screenOffset = control.PointToScreen(Point.Empty);
            DragOver(new Point(e.X - screenOffset.X, e.Y - screenOffset.Y));
        }

        public override event Action<Point> DragOver;

        public override event Action Resized;

        public override event Action<VKey> KeyDown;

        public override event Action<VKey> KeyUp;

        public override event Action Activate;

        public override event Action<Int2> CursorMove;

        public override event Action<int> MouseWheelRotated;

        public event Action<EngineTargetMode> ModeChanged;

        private void ResizeAdapter_ResizeEnd(object sender, EventArgs e)
        {
            if (Resized != null) Resized();
        }

        public override int Width
        {
            get 
            { 
                return TargetMode == EngineTargetMode.Fullscreen ? fullScreenWindow.Width : control.Width; 
            }
        }

        public override int Height
        {
            get 
            {
                return TargetMode == EngineTargetMode.Fullscreen ? fullScreenWindow.Height : control.Height;
            }
        }

        public override IntPtr NativeHandle
        {
            get
            {
                if(currentMode == EngineTargetMode.Fullscreen && FullScreenWindow != null)
                {
                    return (IntPtr)FullScreenWindow.Invoke(new Func<IntPtr>(() => { return FullScreenWindow.Handle; }));
                }
                else
                {
                    IntPtr targetHandle = (IntPtr)control.Invoke(new Func<IntPtr>(() => { return control.Handle; }));
                    if (currentMode == EngineTargetMode.Fullscreen)
                        targetHandle = Win32.GetTopLevelWindowHandle(targetHandle);
                    return targetHandle;
                }
            }
        }

        public override bool IsNativeWindow
        {
            get { return true; }
        }

        public Form FullScreenWindow
        {
            get
            {
                return fullScreenWindow;
            }
            set
            {
                fullScreenWindow = value;
                fullScreenWinInput = new WindowInputListener(fullScreenWindow.Handle, OnWinMessage);
            }
        }

        public override EngineTargetMode TargetMode
        {
            get
            {
                return currentMode;
            }
            set
            {
                if (currentMode != value && ModeChanged != null)
                    ModeChanged.Invoke(value);
                currentMode = value;
            }
        }

        private void OnWinMessage(Message m)
        {
            switch (m.Msg)
            {
                case (int)MsgType.Activate:
                    if (m.WParam.ToInt64() == 0)
                        Activate();
                    break;

                // intercept mouse buttons, convert them to keypress
                case (int)MsgType.LButtonDblClick:
                case (int)MsgType.LButtonDown:
                    KeyDown(VKey.VK_LBUTTON);
                    break;

                case (int)MsgType.LButtonUp:
                    KeyUp(VKey.VK_LBUTTON);
                    break;

                case (int)MsgType.RButtonDown:
                    KeyDown(VKey.VK_RBUTTON);
                    break;

                case (int)MsgType.RButtonUp:
                    KeyUp(VKey.VK_RBUTTON);
                    break;

                // record keypress
                case (int)MsgType.KeyDown:
                    if ((m.LParam.ToInt64() & (int)VKeyState.PreviousState) == 0) // first key-down (otherkey holding events are discarded)
                        KeyDown((VKey)m.WParam.ToInt32());
                    break;

                case (int)MsgType.KeyUp:
                    KeyUp((VKey)m.WParam.ToInt32());
                    break;

                case (int)MsgType.MouseMove:
                    // decode mouse position
                    Int2 cursorCoords;
                    unchecked
                    {
                        uint lparam = (uint)m.LParam.ToInt32();
                        cursorCoords.X = (short)(lparam & 0xffff);
                        cursorCoords.Y = (short)(lparam >> 0x10);
                    }
                    CursorMove(cursorCoords);
                    break;

                case (int)MsgType.MouseWheel:
                    int wheelDelta = unchecked((short)((ulong)m.WParam.ToInt64() >> 0x10));
                    MouseWheelRotated(wheelDelta / 120);
                    break;
            }

        }

    }
}

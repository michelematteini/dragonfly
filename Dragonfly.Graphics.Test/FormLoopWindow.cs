using Dragonfly.Utils;
using DragonflyUtils;
using System;
using System.Windows.Forms;

namespace Dragonfly.Graphics.Test
{
    internal class FormLoopWindow : WindowRenderLoop.IWindow
    {
        private EventHandler idleEventHandler;

        public bool IsIdle
        {
            get { return Win32.IsWindowIdle(); }
        }

        public void SetCallbackOnIdle(Action callback)
        {
            idleEventHandler = (sender, e) => callback();
            Application.Idle += idleEventHandler;
        }

        public void ResetCallbackOnIdle()
        {
            Application.Idle -= idleEventHandler;
        }
    }
}

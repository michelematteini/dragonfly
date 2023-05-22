using System;
using System.Windows.Forms;

namespace Dragonfly.Utils.Forms
{
    public class WindowResizeEndAdapted
    {
        private Timer resizeEndTimer;
        private Control resizableControl;

        public event EventHandler ResizeEnd;

        public WindowResizeEndAdapted(Control resizableControl)
        {
            resizeEndTimer = new Timer();
            resizeEndTimer.Interval = 500;
            resizeEndTimer.Tick += ResizeEndTimer_Tick;

            this.resizableControl = resizableControl;
            resizableControl.Resize += OnResize;
        }

        private void ResizeEndTimer_Tick(object sender, EventArgs e)
        {
            resizeEndTimer.Stop();
            EventHandler resizeEndCallback = ResizeEnd;
            if (resizeEndCallback != null) resizeEndCallback(resizableControl, new EventArgs());
        }

        private void OnResize(object sender, EventArgs e)
        {
            if (resizeEndTimer.Enabled) resizeEndTimer.Stop(); // reset timer
            resizeEndTimer.Start();
        }
    }
}

using System;
using System.Windows.Forms;

namespace Dragonfly.Utils.Forms
{
    public class WindowInputListener : NativeWindow
    {
        private Action<Message> callback;

        public WindowInputListener(IntPtr windowHandle, Action<Message> callback)
        {
            AssignHandle(windowHandle);
            this.callback = callback;
        }

        ~WindowInputListener()
        {
            ReleaseHandle(); 
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            callback(m);
        }

    }
}

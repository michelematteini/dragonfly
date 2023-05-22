using Dragonfly.Engine.Core;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dragonfly.Utils.Forms
{
    public class Panel3D : PictureBox, IControl3D
    {
        private IRenderLoop renderLoop;
        private bool initialized;

        #region IControl3D

        [Category("Dragonfly"), Description("Occurs when an exception is thrown by the game and the CaptureErrors property is set to true.")]
        public event Action<Exception> EngineErrorOccurred;

        [Category("Dragonfly"), Description("Called on game initialization. This event can be used to initilize your scene.")]
        public event Action SceneSetup;

        [Category("Dragonfly"), Description("If true, rendering is performed on the main thread.")]
        public bool RenderOnMainThread { get; set; }

        public EngineContext Engine { get; protected set; }

        [Category("Dragonfly"), Description("Specifies the startup directory for this 3d control. This should be your resource directory.")]
        public string StartupPath { get; set; }

        [Category("Dragonfly"), Description("Choose whether this control should manage exceptions thrown by the rendering thread. If true, when an exception is thrown the rendering loop stops and a EngineErrorOccurred event is rised.")]
        public bool CaptureErrors { get; set; }


        [Category("Dragonfly"), Description("If True, hardware antialiasing willbe used while rendering.")]
        public bool Antialising { get; set; }

        #endregion

        public Panel3D()
        {
            BackColor = System.Drawing.Color.Black;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.Focus();
            base.OnMouseDown(e);
        }

        public void InitializeGraphics()
        {
            if (initialized) return;

            Engine = Control3DLogic.CreateDefaultEngine(this);
            renderLoop = Control3DLogic.CreateRenderLoop(this, renderErrorOccurred);
          
            SceneSetup?.Invoke();
            initialized = true;

            if (Visible)
                renderLoop.Play();
        }

        public void DestroyGraphics()
        {
            renderLoop.Stop();
            Engine.Release();
            thisTarget.TargetMode = EngineTargetMode.Windowed; // exit full screen mode if active
            initialized = false;
        }

        private void renderErrorOccurred(Exception ex)
        {
            EngineErrorOccurred?.Invoke(ex);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (DesignMode) return;

            if (Visible)
            {
                InitializeGraphics();
                renderLoop.Play();
            }
            else if(initialized)
                renderLoop.Pause();
        }

        private TargetControl thisTarget;
        public TargetControl GetTargetControl()
        {
            if (thisTarget == null)
            {
                thisTarget = new TargetControl(this);
                thisTarget.FullScreenWindow = new Form();
                thisTarget.ModeChanged += ThisTarget_ModeChanged;
            }

            return thisTarget;
        }

        private void ThisTarget_ModeChanged(EngineTargetMode mode)
        {
            if (mode == EngineTargetMode.Fullscreen)
            {
                thisTarget.FullScreenWindow.Visible = true;
            }
            else
            {
                Task.Delay(1000).ContinueWith((t) => thisTarget.FullScreenWindow.Invoke(new Action(() =>
                {
                    thisTarget.FullScreenWindow.Visible = false;
                })));
            }
        }
    }
}

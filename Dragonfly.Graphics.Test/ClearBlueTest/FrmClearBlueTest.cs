using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System;
using System.Windows.Forms;

namespace Dragonfly.Graphics.Test
{
    public partial class FrmClearBlueTest : Form, IConsoleProgram
    {
        private IDFGraphics g;
        private WindowRenderLoop renderLoop;
        private CommandList cmdList;

        public string ProgramName
        {
            get
            {
                return string.Format("Clear Blue ({0})", GraphicsAPIs.GetDefault().Description);
            }
        }

        public FrmClearBlueTest()
        {
            InitializeComponent();
            renderLoop = new WindowRenderLoop(new FormLoopWindow());
            renderLoop.FrameRequest += RenderLoop_FrameRequest;
            renderLoop.ResumeAttempt += RenderLoop_ResumeAttempt;
        }

        private void RenderLoop_ResumeAttempt(ResumeLoopEventArgs e)
        {
            e.ResumeSucceeded = g.IsAvailable;
        }

        private void RenderLoop_FrameRequest(RenderLoopEventArgs e)
        {
            if (!g.NewFrame())
            {
                //device not ready, pause rendering
                e.TryResume = true;
                return;
            }
            cmdList.StartRecording();
            cmdList.ClearSurfaces(Color.Blue.ToFloat4(), ClearFlags.ClearTargets | ClearFlags.ClearDepth);
            cmdList.QueueExecution();
            g.StartRender();
            g.DisplayRender();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            renderLoop.Stop();
            if (g != null) g.Release();
        }

        private void OnResizeEnd(object sender, EventArgs e)
        {
            g.SetScreen(pnlTarget.Handle, false, pnlTarget.Width, pnlTarget.Height);
        }

        public void RunProgram()
        {
            //create graphics
            DFGraphicSettings gsettings = new DFGraphicSettings();
            gsettings.FullScreen = false;
            gsettings.PreferredWidth = pnlTarget.Width;
            gsettings.PreferredHeight = pnlTarget.Height;
            gsettings.TargetControl = pnlTarget.Handle;
            gsettings.ResourceFolder = PathEx.DefaultResourceFolder;
            g = GraphicsAPIs.GetDefault().CreateGraphics(gsettings);

            if (!g.IsAvailable) throw new Exception(string.Format("The currently used API ({0}) is unavailable.", GraphicsAPIs.GetDefault().Description));
            cmdList = g.CreateCommandList();
            renderLoop.Play();
            this.ShowDialog();
        }


    }
}

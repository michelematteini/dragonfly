using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Dragonfly.Graphics.Test.ResourceAllocTest
{
    public partial class FrmAllocationTest : Form, IConsoleProgram
    {
        private IDFGraphics g;
        private WindowRenderLoop renderLoop;

        private List<VertexBuffer> vbList;
        private List<Texture> texList;
        private List<Shader> shdList;
        private CommandList cmdList;

        public FrmAllocationTest()
        {
            InitializeComponent();
            renderLoop = new WindowRenderLoop(new FormLoopWindow());
            renderLoop.FrameRequest += RenderLoop_FrameRequest;
            renderLoop.ResumeAttempt += RenderLoop_ResumeAttempt;
        }

        public string ProgramName
        {
            get
            {
                return string.Format("Allocation Test ({0})", GraphicsAPIs.GetDefault().Description);
            }
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
            cmdList.ClearSurfaces(new Float4(0, 1, 0, 1), ClearFlags.ClearTargets | ClearFlags.ClearDepth);
            cmdList.QueueExecution();
   
            g.StartRender();

            g.DisplayRender();
        }

        private void LoadResources()
        {
            //CREATE RESOURCES

            // vertex buffer
            vbList = new List<VertexBuffer>();
            VertexType colorVertex = new VertexType(VertexElement.Position4, VertexElement.Float4);
            for ( int i = 0; i < 1000; i++)
            {
                VertexBuffer vertices = g.CreateVertexBuffer(colorVertex, 3);
                vertices.SetVertices<VertexColorTex>(new VertexColorTex[]
                {
                new VertexColorTex(new Float3(-0.5f, -0.5f, 0), new Float3("#FFA000")),
                new VertexColorTex(new Float3(0.5f, -0.5f, 0), new Float3("#00FF00")),
                new VertexColorTex(new Float3(0.5f, 0.5f, 0), new Float3("#00A0FF"))
                });
                vbList.Add(vertices);
            }

            // textures
            texList = new List<Texture>();
            byte[] testTextureBytes = File.ReadAllBytes(PathEx.GetDefaultResorcePath("textures/green_grass1.dds"));
            for (int i = 0; i < 100; i++)
            {
                texList.Add(g.CreateTexture(testTextureBytes));
            }

            // shaders
            shdList = new List<Shader>();
            for (int i = 0; i < 100; i++)
            {
                shdList.Add(g.CreateShader("SimpleColorEffect", ShaderStates.Default, null));
            }

            cmdList = g.CreateCommandList();
        }

        private void ReleaseResources()
        {
            foreach (VertexBuffer vb in vbList) vb.Release();
            foreach (Texture tex in texList) tex.Release();
            foreach (Shader shd in shdList) shd.Release();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            renderLoop.Stop();
            if (g != null) g.Release();
        }

        public void RunProgram()
        {
            //create graphics
            DFGraphicSettings gsettings = new DFGraphicSettings();
            gsettings.FullScreen = false;
            gsettings.PreferredWidth = this.Width;
            gsettings.PreferredHeight = this.Height;
            gsettings.TargetControl = this.Handle;
            gsettings.ResourceFolder = PathEx.DefaultResourceFolder;
            g = GraphicsAPIs.GetDefault().CreateGraphics(gsettings);


            LoadResources();
            ReleaseResources();
            LoadResources();
            ReleaseResources();
            LoadResources();
            ReleaseResources();
            LoadResources();
            ReleaseResources();

            renderLoop.Play();
            this.ShowDialog();
        }
    }
}

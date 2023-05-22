using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System;
using System.Windows.Forms;

namespace Dragonfly.Graphics.Test
{
    public partial class FrmTriangleTest : Form, IConsoleProgram
    {
        private IDFGraphics g;
        private VertexType colorTexVertex;
        private VertexBuffer vertices;
        private Shader shader;
        private IndexBuffer indices;
        private Texture grassTexture;
        private WindowRenderLoop renderLoop;
        private int initialTicks;
        private CommandList cmdList;

        public string ProgramName
        {
            get
            {
                return string.Format("{1} Triangle Test ({0})", GraphicsAPIs.GetDefault().Description, ApplyTexture ? "Textured" : "Colored");
            }
        }

        public FrmTriangleTest()
        {
            InitializeComponent();
            renderLoop = new WindowRenderLoop(new FormLoopWindow());
            renderLoop.FrameRequest += RenderLoop_FrameRequest;
            renderLoop.ResumeAttempt += RenderLoop_ResumeAttempt;
            initialTicks = Environment.TickCount;
        }

        public bool ApplyTexture { get; set; }

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
            cmdList.ClearSurfaces(Float4.UnitW, ClearFlags.ClearTargets | ClearFlags.ClearDepth);
            cmdList.SetVertices(vertices);
            cmdList.SetIndices(indices);
          
            // update shader uniforms
            shader.SetParam("viewMatrix", Float4x4.LookAt(new Float3(0, 0, -3.0f), Float3.UnitZ, Float3.UnitY));
            shader.SetParam("projectionMatrix", Float4x4.Perspective((float)(System.Math.PI / 4.0), 1.0f, 0.1f, 100.0f));  
            shader.SetParam("texGrass", grassTexture);
            float time = (float)(Environment.TickCount - initialTicks) * 0.001f;
            shader.SetParam("rotation", Float4x4.RotationY(time));
            cmdList.SetShader(shader);
            cmdList.DrawIndexed();
            cmdList.QueueExecution();

            g.StartRender();

            g.DisplayRender();
        }

        private void LoadResources()
        {
            //CREATE RESOURCES
            colorTexVertex = new VertexType(VertexElement.Position3, VertexElement.Float3, VertexElement.Float2);
            vertices = g.CreateVertexBuffer(colorTexVertex, 3);
            vertices.SetVertices<VertexColorTex>(new VertexColorTex[]
            {
                new VertexColorTex(new Float3(-0.5f, -0.5f, 0), new Float3("#FFA000"), Float2.Zero),
                new VertexColorTex(new Float3(0.5f, -0.5f, 0), new Float3("#00FF00"), Float2.UnitX),
                new VertexColorTex(new Float3(0.5f, 0.5f, 0), new Float3("#00A0FF"), Float2.One)
            });
            indices = g.CreateIndexBuffer(3);
            indices.SetIndices(new ushort[] { 1, 0, 2 });
            grassTexture = g.CreateTexture(System.IO.File.ReadAllBytes(PathEx.GetDefaultResorcePath("textures/green_grass1.dds")));
            ShaderStates states = ShaderStates.Default;
            states.CullMode = CullMode.None;
            shader = g.CreateShader(ApplyTexture ? "SimpleColorTexEffect" : "SimpleColorEffect", states, null);
            cmdList = g.CreateCommandList();
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

            LoadResources();

            renderLoop.Play();
            this.ShowDialog();
        }
    }
}

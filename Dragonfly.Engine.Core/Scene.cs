using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dragonfly.Engine.Core
{
    public class Scene
    {
        private const int MAX_VIEW_COUNT = 1024;

        private IDFGraphics graphics;
        private EngineTarget target;
        private EngineContext context;
        private bool resolutionUpdateRequested, targetChangedResolution;
        private HashSet<CompRenderPass> renderedViews;
        private Stack<CompRenderPass> passStack;
        private object renderLock;
        private bool sceneRenderedToScreen; // true if at least one pass has rendered to screen in the current frame
        private EngineResourceAllocator resAllocator;

#if VERBOSE
        internal SceneLog Log;
#endif

        internal Scene(EngineContext context, EngineTarget target)
        {
            this.context = context;
            this.target = target;
            Components = new ComponentManager();
            Root = new Component(context, Components);
            Root.IsRoot = true;
            Root.Name = "ROOT";
            renderedViews = new HashSet<CompRenderPass>();
            passStack = new Stack<CompRenderPass>();
            renderLock = new object();
            RenderingEnabled = true;

            // create a default view pass basic forward rendering
            if (target.IsNativeWindow)
                MainRenderPass = new CompRenderPass(Root, "DefaultPass"); // render to screen
            else if(target.Width != 0 && target.Height != 0)
                MainRenderPass = new CompRenderPass(Root, "DefaultPass", new CompRenderBuffer(Root, SurfaceFormat.Color, RenderBufferResizeStyle.MatchBackbuffer)); // render to a texture that match the target size

            // create settings
            Settings = new DFGraphicSettings();
            Settings.FullScreen = false;

#if VERBOSE
            Log = new SceneLog();
#endif
        }

#region Properties

        /// <summary>
        /// Returns whether the scene graphics have bee initialized successfully
        /// </summary>
        internal bool Initialized { get { return graphics != null; } }

        internal IDFGraphics Graphics { get { return graphics; } }

        /// <summary>
        /// The component manager used to access and query all the components used in this scene.
        /// </summary>
        internal ComponentManager Components { get; private set; }

        /// <summary>
        /// A components from which the scene tree starts. All components in this scene must have this component as an anchestor.
        /// </summary>
        public Component Root { get; private set; }

        /// <summary>
        /// This is the final pass of the rendering pipeline that renders the final frame image.
        /// A single frame is rendered by performing recursively all the passes required by this pass.
        /// </summary>
        public CompRenderPass MainRenderPass { get; set; }

        /// <summary>
        /// Enable or disable the scene rendering, without affecting the engine update pipeline which can keep running.
        /// </summary>
        public bool RenderingEnabled { get; set; }

        /// <summary>
        /// List the graphics settings currently used by this scene instance.
        /// </summary>
        internal DFGraphicSettings Settings { get; }


        internal CommandList MainCommandList { get; private set; }

        /// <summary>
        /// Modifiers for updating shaders global parameters.
        /// </summary>
        public EngineGlobals Globals { get; private set; }

        #endregion

        internal RenderStats LastFrameStats { get; private set; }

        internal bool Initialize()
        {
            if (Initialized) return true;
            if (!InitializeGraphics()) return false;               
            target.Resized += Target_Resized;
            UpdateResolution();

            return true;                        
        }

        private bool InitializeGraphics()
        {
            Settings.ResourceFolder = context.ResourceFolder;
            if (Settings.PreferredWidth == 0 || Settings.PreferredHeight == 0)
            {
                Settings.PreferredWidth = target.Width;
                Settings.PreferredHeight = target.Height;
            }
            target.TargetMode = Settings.FullScreen ? EngineTargetMode.Fullscreen : EngineTargetMode.Windowed;
            Settings.TargetControl = target.IsNativeWindow ? target.NativeHandle : IntPtr.Zero;
            graphics = GraphicsAPIs.GetDefault().CreateGraphics(Settings);
            resAllocator = new EngineResourceAllocator(graphics);
            MainCommandList = resAllocator.CreateCommandList();
            Globals = new EngineGlobals(MainCommandList);
            return Initialized;
        }

        /// <summary>
        /// Prepare and render the current frame of this scene.
        /// </summary>
        /// <returns></returns>
        internal virtual bool RenderFrame()
        {
            // initialize engine graphics if its still not available
            if (!Initialized)
                Initialize();

            // update pipeline resolution, also triggering resizable components
            UpdateResolution();

            // signal start of frame
            if (!graphics.NewFrame()) 
                return false;

#if TRACING
            Graphics.StartTracedSection(Color.Magenta, "Scene");
#endif

#if VERBOSE
            Log.FrameStart(context.Time.FrameIndex);
#endif

            // start tracking constants updates
            MainCommandList.StartRecording();

#if TRACING
            Graphics.StartTracedSection(Color.Cyan, "PrepareFrame");
#endif

            // update component values
            Components.OnNewFrameStart();

#if TRACING
            Graphics.StartTracedSection(Color.Cyan, "UpdateType.FrameStart1");
#endif
            // call updatable components
            Components.UpdateComponents(Graphics, UpdateType.FrameStart1);

#if TRACING
            Graphics.EndTracedSection();
            Graphics.StartTracedSection(Color.Cyan, "UpdateType.FrameStart2");
#endif

            // call updatable components
            Components.UpdateComponents(Graphics, UpdateType.FrameStart2);

#if TRACING
            Graphics.EndTracedSection();
            Graphics.StartTracedSection(Color.Cyan, "LoadGraphicResources");
#endif

            // load needed resources
            Components.LoadComponentResources(Graphics, resAllocator);

#if TRACING
            Graphics.EndTracedSection();
            Graphics.StartTracedSection(Color.Cyan, "UpdateType.ResourceLoaded");
#endif

            // call updatable components after resources have been loaded
            Components.UpdateComponents(Graphics, UpdateType.ResourceLoaded);

#if TRACING
            Graphics.EndTracedSection();
#endif

            //  update scene shader globals
            Globals.SetParam("preciseSeconds", context.Time.SecondsFromStart.ToFloat2());

#if TRACING
            Graphics.EndTracedSection();
#endif
            MainCommandList.QueueExecution();

#if VERBOSE
            if (MainRenderPass == null) // nothing to render?
                Log.WriteLine("No pass set on Scene.MainRenderPass! Nothing to be rendered!");     
#endif

            if (RenderingEnabled && MainRenderPass != null)
            {
                lock (renderLock)
                {
                    // render all passes
                    RenderAllPasses();

                    // signal end of frame
                    graphics.StartRender();

                    // display render on screen
                    if (sceneRenderedToScreen)
                        graphics.DisplayRender();
                }
            }

            UpdateFrameStats();

#if TRACING
            Graphics.EndTracedSection();
#endif


            return true;
		}

        /// <summary>
        /// render all the passes needed for the current frame
        /// </summary>
        private void RenderAllPasses()
        {
            sceneRenderedToScreen = false;
            renderedViews.Clear();
            RenderPassTree(MainRenderPass);
        }

        private void UpdateFrameStats()
        {
            RenderStats curFrameStats = new RenderStats();
            foreach (CompRenderPass pass in renderedViews)
                curFrameStats += pass.Stats;
            LastFrameStats = curFrameStats;
        }

        /// <summary>
        /// Render the specified pass and all the required passes recursively.
        /// </summary>
        private void RenderPassTree(CompRenderPass presentationPass)
        {
            passStack.Push(presentationPass);

            while(passStack.Count > 0)
            {
                if (renderedViews.Count > MAX_VIEW_COUNT)
                    throw new Exception("View-stack overflow, too many rendered in this frame! This can be caused by too many queued views or by a circular dependency on the active views.");

                CompRenderPass currentPass = passStack.Pop();
                if (currentPass.RequiredPasses.Count > 0)
                {
                    // queue views required by the current one
                    for (int i = currentPass.RequiredPasses.Count - 1; i >= 0 ; --i)
                    {
                        CompRenderPass requiredPass = currentPass.RequiredPasses[i];

                        // remove disposed passes, that are still listed as required
                        // (this allow user to not worry about dangling required views references)
                        if(requiredPass.Disposed)
                        {
                            currentPass.RequiredPasses.RemoveAt(i);
                            continue;
                        }

                        // if the required can be rendered, add to the pass stack 
                        if (requiredPass.CanBeRendered && !renderedViews.Contains(requiredPass))
                            passStack.Push(requiredPass);
                    }
                }

                // save if at least one pass has rendered to screen
                if (!currentPass.RenderToTexture || !currentPass.CanRenderToTexture)
                    sceneRenderedToScreen = true; 

                // render the current view     
                currentPass.Render();
                renderedViews.Add(currentPass);            
            }
        }

        /// <summary>
        /// Release all graphics resources loaded for this scene.
        /// </summary>
        private void ReleaseGraphicResources()
		{
            Globals.Release();
            Globals = null;
            Components.ReleaseComponentResources();
        }

        /// <summary>
        /// Destroy the current scene releasing all associated resources.
        /// </summary>
        internal void Release()
        {
            target.Resized -= Target_Resized;
            ReleaseGraphicResources();
            if (Initialized)
                graphics.Release();
            Root.Dispose();
            Components.Clear();
        }

#region Rendering Resolution

        public Int2 Resolution
        {
            get
            {
                // if the scene is still not initialized, use the target size as scene resolution (may be required for scene setup)
                if (graphics == null)
                    return new Int2(target.Width, target.Height);

                return new Int2(Settings.PreferredWidth, Settings.PreferredHeight);
            }
            set
            {
                if (value.X > 0 && value.Y > 0)
                {
                    ResizeStyle = ResizeStyle.KeepResolution;
                    Settings.PreferredWidth = value.X;
                    Settings.PreferredHeight = value.Y;

                    if (Initialized)
                        resolutionUpdateRequested = true;
                }
            }
        }

        public List<Int2> SupportedFullScreenResolutions
        {
            get { return graphics.SupportedDisplayResolutions; }
        }

        public bool FullScreen
        {
            get { return Settings.FullScreen; }
            set
            {
                if (Settings.FullScreen == value) return;
                
                Settings.FullScreen = value;                   
                resolutionUpdateRequested = true;

                if (Settings.FullScreen) // switch off auto resize on full screen mode
                    ResizeStyle = ResizeStyle.KeepResolution;
            }
        }

        public void ForceViewportUpdate()
        {
            resolutionUpdateRequested = true;
        }

        public ResizeStyle ResizeStyle { get; set; }

        private void Target_Resized()
        {
            if (target.Width <= 0 || target.Height <= 0)
                return; // invalid target resolution

            if (ResizeStyle == ResizeStyle.Automatic)
            {
                Settings.PreferredWidth = target.Width;
                Settings.PreferredHeight = target.Height;
                resolutionUpdateRequested = true;
            }
            else
            {
                targetChangedResolution = true;
            }
        }

        private void UpdateResolution()
        {
            if (resolutionUpdateRequested)
            {
                target.TargetMode = Settings.FullScreen ? EngineTargetMode.Fullscreen : EngineTargetMode.Windowed;
                graphics.SetScreen(target.IsNativeWindow ? target.NativeHandle : IntPtr.Zero, Settings.FullScreen, Settings.PreferredWidth, Settings.PreferredHeight);
            }

            if (resolutionUpdateRequested || targetChangedResolution)
            {
                foreach (ICompResizable c in Components.Query<ICompResizable>())
                {
                    if (!c.Active) continue;
                    c.ScreenResized(graphics.CurWidth, graphics.CurHeight);
                }
            }

            resolutionUpdateRequested = false;
            targetChangedResolution = false;
        }

#endregion
    }

    public enum ResizeStyle
    {
        Automatic = 0,
        KeepResolution
    }

}

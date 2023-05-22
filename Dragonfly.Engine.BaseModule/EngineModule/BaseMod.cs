using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    public class BaseMod : EngineModule
    {
        /// <summary>
        /// Specify the type of rendering that the user intend to mainly perform with the components of this module.
        /// </summary>
        public enum Usage
        {
            /// <summary>
            /// Empty BaseMod environment, only helper components are loaded and no rendering pipeline is created.
            /// </summary>
            Unspecified = 0,
            /// <summary>
            /// Generic 3d rendering configuration.
            /// </summary>
            Generic3D,
            /// <summary>
            /// Configuration for PBR usage.
            /// </summary>
            PhysicalRendering
        }

        private bool initialized;
        private CompUiCtrlPicture img3DRender; // the background UI control containing the final rendered image, without UI. 

        public BaseMod()
        {
            Settings = BaseModSettings.Default;
            initialized = false;
        }

        public BaseModSettings Settings { get; set; }

        public Usage CurrentUsage { get; private set; }

        public override void OnModuleAdded()
        {
        }

        /// <summary>
        /// Configure this module components to the suggested defaults for a give type of rendering. 
        /// </summary>
        public void Initialize(Usage usage)
        {
            if (initialized)
                throw new InvalidOperationException("The Base Module has already been initalized!");

            if (Context == null)
                throw new InvalidOperationException("This module must be added to a graphic context before this call can be made.");

            Component baseModuleRoot = Context.Scene.Root;

            switch (usage)
            {
                case Usage.Unspecified:
                    InitializeEmptyEnvironment(baseModuleRoot);
                    break;

                case Usage.Generic3D:
                    InitializeForGeneric3D(baseModuleRoot);
                    break;
                case Usage.PhysicalRendering:
                    InitializeForPhysicalRendering(baseModuleRoot);
                    break;
            }

            CurrentUsage = usage;
            initialized = true;
        }

        private void InitializeEmptyEnvironment(Component root)
        {
            InitializeInputDevices();
            InitializeSharedComponents(root);
        }

        private void InitializeForGeneric3D(Component root)
        {
            InitializeInputDevices();
            InitializeSharedComponents(root);
            InitializeForward3DPipelineWithUI(root);
            Initialize3DSharedComponents(root);
        }

        private void InitializeForPhysicalRendering(Component root)
        {
            InitializeInputDevices();
            InitializeSharedComponents(root);
            InitializeForward3DPipelineWithUI(root);
            Initialize3DSharedComponents(root);
            PostProcess.ColorEncoding = OuputColorEncoding.Reinhard;
        }

        private void InitializeSharedComponents(Component root)
        {
            new CompTextureLoader(root);
            new CompTaskScheduler(root);
            new CompMeshGeomBuffers(root);
            new CompObjToMesh(root);
            new CompInputFocus(root);
            new CompRandom(root);
        }

        private void Initialize3DSharedComponents(Component root)
        {      
            Sound = new CompAudioEngine(root);
            new CompLightTableManager(root);
            new CompIndirectLightManager(root);
            new CompMeshPicker(root);
            LoadingScreen = new CompUiLoadingScreen(root);
            CompShadowAtlas shadows = new CompShadowAtlas(root, MainPass);
            ShadowAtlas = shadows.ShadowAtlas.RenderBuffer;
        }

        private void InitializeInputDevices()
        {
            Keyboard keyboard = new Keyboard(Context.TargetWindow);
            Context.Input.AddDevice(keyboard);
            Context.Input.AddDevice(new Mouse(Context.TargetWindow, keyboard));
        }

        private void InitializeForward3DPipelineWithUI(Component root)
        {
            // create a buffer that will be used by both solid and depth pre-pass to share the same z-buffer
            CompRenderBuffer gBuffer = new CompRenderBuffer(root, new SurfaceFormat[] { SurfaceFormat.Color /*HDR Color*/, SurfaceFormat.Half /* SS Depth */}, RenderBufferResizeStyle.MatchBackbuffer);

            // Solid forward pass
            {
                CompRenderPass solidPass = new CompRenderPass(root, "MainSolidPass", gBuffer);
                solidPass.MaterialFilters.Add(new MaterialClassFilter(MaterialClassFilterType.Include, Settings.MaterialClasses.Solid));
                solidPass.ActiveBufferIndices = new int[] { 0 };
                solidPass.ClearFlags =  ClearFlags.ClearTargets;
                solidPass.Camera = new CompCamIdentity(solidPass); // add an empty camera to make this pass run even without user cameras (to at leas clear the background).
                MainPass = solidPass;
                LastCompositionPass = solidPass;
            }

            // G-buffer depth prepass
            {
                DepthPrepass = new CompRenderPass(root, "DepthPrepass", gBuffer);
                DepthPrepass.MaterialFilters.Add(new MaterialClassFilter(MaterialClassFilterType.Include, Settings.MaterialClasses.Solid));
                DepthPrepass.ActiveBufferIndices = new int[] { 1 };
                DepthPrepass.ClearValue = Float4.Zero; // this will be the farthest z since its inverted in shader
                DepthPrepass.OverrideShaderTemplate = Settings.ShaderTemplates.DepthPrePass;
                MainPass.RequiredPasses.Add(DepthPrepass);
                DepthPrepass.LinkCameraListWith(MainPass); // depth prepass will always render the same exact scene as the main render pass
            }

            // To screen pass: copy the rendered image to screen, used by default to also render UI
            {
                // Render to screen and gui pass
                ToScreenPass = new CompRenderPass(root, "ToScreen");
                ToScreenPass.MainClass = Settings.MaterialClasses.UI;
                ToScreenPass.ClearValue = Color.Red.ToFloat4();
                ToScreenPass.Camera = new CompCamIdentity(ToScreenPass);
                ToScreenPass.RequiredPasses.Add(LastCompositionPass);

                // Default UI panel
                UiContainer = new CompUiContainer(root, new UiRenderPassCanvas(ToScreenPass), "100% 100%", "0px 0px", PositionOrigin.TopLeft);

                // Add the 3D rendering as background image with maximum priority in the screen rendering pass
                PostProcess = new CompMtlPostProcess(UiContainer);
                img3DRender = new CompUiCtrlPicture(UiContainer, PostProcess);
                img3DRender.Image.SetSource(new RenderTargetRef(LastCompositionPass.RenderBuffer));
                img3DRender.ImageMaterial.RenderOrder = UiZIndex.BottomRenderOrder - 10; // display rendering under the UI

                // replace the engine main pass with this module screen pass
                Context.Scene.MainRenderPass.Dispose();
                Context.Scene.MainRenderPass = ToScreenPass;
            }

            // Additional passes, linked to the main ones
            CompDirectionalLightFilterPass lightFilterPass = new CompDirectionalLightFilterPass(root);
        }

        public CompUiLoadingScreen LoadingScreen { get; private set; }

        public CompMtlPostProcess PostProcess { get; private set; }

        internal CompRenderBuffer ShadowAtlas { get; private set; }

        /// <summary>
        /// A pass that only render depth, and pre-fill the z-buffer.
        /// </summary>
        public CompRenderPass DepthPrepass { get; private set; }

        /// <summary>
        /// The view that will be used to transform the geometry actually displayed on screen.
        /// This view will be used as reference from this module for shadows, sounds, lights, etc.
        /// </summary>
        public CompRenderPass MainPass { get; private set; }

        public Int3 CurWorldTile
        {
            get
            {
                return MainPass.Camera.GetTransform().Tile;
            }
        }

        /// <summary>
        /// Default UI and 2d rendering pass. Performed after any 3d rendering.
        /// </summary>
        public CompRenderPass ToScreenPass { get; private set; }

        /// <summary>
        /// The last rendering pass before drawing to screen, that render the final 3d-only scene.
        /// </summary>
        public CompRenderPass LastCompositionPass { get; private set; }

        /// <summary>
        /// Add a new composition pass to the rendering pipeline that is performed just before displaying it to screen.
        /// </summary>
        /// <returns>The previous composition pass, that can be used as input to the newly added pass.</returns>
        public CompRenderPass PushCompositionPass(CompRenderPass pass)
        {
            // attach ToScreen pass to this new composition pass
            if (LastCompositionPass != null)
                ToScreenPass.RequiredPasses.Remove(LastCompositionPass);
            img3DRender.Image.SetSource(new RenderTargetRef(pass.RenderBuffer));
            ToScreenPass.RequiredPasses.Add(pass);
            
            // make the new pass "require" the previous
            pass.RequiredPasses.Add(LastCompositionPass);

            // replace last composition pass
            CompRenderPass previousCompPass = LastCompositionPass;
            LastCompositionPass = pass;

            return previousCompPass;
        }

        public CompUiContainer UiContainer { get; private set; }

        public CompAudioEngine Sound { get; private set; }

        /// <summary>
        /// Create a screen quad mesh.
        /// </summary>
        public static CompMesh CreateScreenMesh(Component parent)
        {
            CompMesh screenMesh = new CompMesh(parent);
            Primitives.ScreenQuad(screenMesh.AsObject3D());
            screenMesh.IsBounded = false;
            screenMesh.CastShadows = false;
            return screenMesh;
        }

    }

}

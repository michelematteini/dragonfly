using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A render pass that prepare a screen space filtering buffer for a specific directional light, including atmospheric-related effects.
    /// </summary>
    internal class CompDirectionalLightFilterPass : Component, ICompUpdatable
    {
        public CompLightDirectional CurrentLight { get; private set; }
        
        public CompDirectionalLightFilterPass(Component parent) : base(parent)
        {
            BaseMod baseMod = Context.GetModule<BaseMod>();

            CompRenderBuffer renderBuffer = new CompRenderBuffer(this, Graphics.SurfaceFormat.Color, RenderBufferResizeStyle.HalfBackbuffer);
            Pass = new CompRenderPass(this, "DirectionalLightFilterPass", renderBuffer);
            Pass.MainClass = baseMod.Settings.MaterialClasses.DirectionalLightFilter;
            Pass.ClearValue = Float4.One;
            Pass.ClearFlags = ClearFlags.ClearTargets;
            Pass.DebugColor = Color.Cyan; ;
            Pass.RequiredPasses.Add(baseMod.DepthPrepass);
            Pass.LinkCameraListWith(baseMod.MainPass);
            baseMod.MainPass.RequiredPasses.Add(Pass);
            LightFilterTexture = new CompTextureRef(this, Color.White);
            LightFilterTexture.SetSource(new RenderTargetRef(renderBuffer));
        }

        public CompRenderPass Pass { get; private set; }

        public CompTextureRef LightFilterTexture { get; private set; }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        public void Update(UpdateType updateType)
        {
            // update current light
            CurrentLight = null;
            int curLightIndex = -1;
            IReadOnlyList<CompLightDirectional> dirLightList = GetComponents<CompLightDirectional>();
            for (int i = 0; i < dirLightList.Count; i++)
            {
                if (CurrentLight == null || CurrentLight.Intensity.GetValue() < dirLightList[i].Intensity.GetValue())
                {
                    CurrentLight = dirLightList[i];
                    // NB this index will have to match the light table one!
                    // here it works because the same query with GetComponents<CompLightDirectional>() is used
                    curLightIndex = i; 
                }
            }

            // update globals
            Context.Scene.Globals.SetParam("lightFilterTex", LightFilterTexture);
            Context.Scene.Globals.SetParam("filteredLightID", curLightIndex);
        }
    }
}

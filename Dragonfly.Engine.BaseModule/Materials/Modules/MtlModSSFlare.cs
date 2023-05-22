using System;
using System.Collections.Generic;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;

namespace Dragonfly.BaseModule
{
    public class MtlModSSFlare : MaterialModule
    {
        private const int MAX_FLARE_COUNT = 8;

        // ss location and exposure of all the flares to be rendered
        private Float2[] ssflareLightLoc;
        private Float3[] ssflareLightColor;
        private int ssflareCount;

        private BaseMod baseMod;

        public MtlModSSFlare(CompMaterial parentMaterial) : base(parentMaterial)
        {
            ssflareLightLoc = new Float2[MAX_FLARE_COUNT];
            ssflareLightColor = new Float3[MAX_FLARE_COUNT];
            ScatteredPercent = MakeParam(0.4f);
            Decay = MakeParam(1024.0f);
            RaysCounts = MakeParam(new Float4(5.0f, 7.0f, 2.0f, 11.0f));
            RaysIntensity = MakeParam(1.0f);
            RaysCentralDecay = MakeParam(1024.0f);
            Lights = new List<CompLight>();
            baseMod = Context.GetModule<BaseMod>();
        }

        /// <summary>
        /// Percentage of light scattered to form the flare. 
        /// </summary>
        public CompMaterial.Param<float> ScatteredPercent;
        /// <summary>
        /// How fast the flare intensity decay while moving away from the light position
        /// </summary>
        public CompMaterial.Param<float> Decay;
        /// <summary>
        /// Number of rays drawn from the light position, each component can contain a different number, these are then averaged toghether
        /// </summary>
        public CompMaterial.Param<Float4> RaysCounts;
        /// <summary>
        /// How marked the rays are 0 = not visible 1 = modulate all the light
        /// </summary>
        public CompMaterial.Param<float> RaysIntensity;
        /// <summary>
        /// Blend rays to the light center, higher values make the rays start closer to the light
        /// </summary>
        public CompMaterial.Param<float> RaysCentralDecay;

        public List<CompLight> Lights { get; private set; }

        private void UpdateLightCache()
        {
            int lightCount = Math.Min(Lights.Count, ssflareLightLoc.Length);
            CompCamera camera = baseMod.MainPass.Camera;
            TiledFloat4x4 cameraTransform = camera.GetTransform();
            Float4x4 cameraMatrix = cameraTransform.Value * camera.GetValue();
            ssflareCount = 0;

            for (int i = 0; i < lightCount; i++)
            {
                ssflareLightColor[ssflareCount] = Float3.Zero; // disabled by default

                if (Lights[i] is CompLightDirectional dirLight)
                {
                    Float4 lightDir = new Float4(-dirLight.Direction, 0);
                    Float4 ssLightDir = lightDir * cameraMatrix;
                    ssflareLightLoc[ssflareCount] = new Float2(0.5f, -0.5f) * ssLightDir.XY / ssLightDir.W + 0.5f;
                    Float3 lightColor = dirLight.LightColor.GetValue();
                    CompAtmoLightFilter lightFilter = dirLight.GetFirstChild<CompAtmoLightFilter>();
                    if (lightFilter != null) // use the filtered light color if the light is currently filtered by atmospheres
                        lightColor = lightFilter.GetValue();
                    ssflareLightColor[ssflareCount] = dirLight.Intensity.GetValue() * lightColor;
                    ssflareLightColor[ssflareCount] *= (2.0f * ssLightDir.W).Saturate(); // fade out when looking aways from the light
                }

                if (ssflareLightColor[ssflareCount].Any())
                    ssflareCount++;
            }
        }

        protected override void UpdateAdditionalParams(Shader s)
        {
            s.SetParam("ssflareScatteredPercent", ScatteredPercent);
            s.SetParam("ssflareDecay", Decay);
            s.SetParam("ssflareRaysCounts", RaysCounts);
            s.SetParam("ssflareRaysIntensity", RaysIntensity);
            s.SetParam("ssflareRaysCentralDecay", RaysCentralDecay);
            UpdateLightCache();
            s.SetParam("ssflareLightLoc", ssflareLightLoc);
            s.SetParam("ssflareLightColor", ssflareLightColor);
            s.SetParam("ssflareLightCount", ssflareCount);

        }
    }
}

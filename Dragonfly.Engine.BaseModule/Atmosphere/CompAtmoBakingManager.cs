using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics;
using System;
using System.Collections.Generic;
using Dragonfly.Utils;

namespace Dragonfly.BaseModule.Atmosphere
{
    /// <summary>
    /// Coodinates baking operations for all the active atmospheres.
    /// </summary>
    public class CompAtmoBakingManager : Component, ICompUpdatable
    {
        private static readonly string ATMO_DIST_LUT_PASS = "AtmosphereOpticalDistLUTBaking";
        private static readonly string ATMO_IRRADIANCE_LUT_PASS = "AtmosphereIrradianceLUTBaking";
        private const int IRRADIANCE_LUT_RES = 32;
        private const int ATMO_IRRADIANCE_SPLITS = 3;

        private enum AtmosphereBakingStage
        {
            ToBeBaked = 0,
            Baking = 1,
            BakedStep1 = 2,
            BakingCompleted = 3
        }

        private class AtmosphereBakingState
        {
            public CompAtmosphere Atmosphere;
            public CompCamera DistLutCamera;
            public CompMtlAtmosphereDepthLUT DistLutMaterial;
            public CompCamera IrradianceLutCamera;
            public CompMtlAtmosphereIrradianceLUT IrradianceLutMaterial;
            public CompRenderBuffer IrradianceRenderBuffer;
            public AtmosphereBakingStage Stage;
        }

        private TextureAtlas atmoOpticalDistAtlas;
        private TextureAtlas atmoIrradianceAtlas;
        private List<AtmosphereBakingState> atmoBakingStates;


        public CompAtmoBakingManager(Component parent) : base(parent)
        {
            BaseMod baseMod = Context.GetModule<BaseMod>();
            atmoOpticalDistAtlas = new TextureAtlas(this, "AtmosphereOpticalDistLUT", ATMO_DIST_LUT_PASS, SurfaceFormat.Float2, new AtlasLayoutFixedGrid(new Int2(32, 512), new Int2(CompAtmosphere.MAX_DISPLAYED_COUNT, 1)));
            atmoOpticalDistAtlas.Pass.DebugColor = Color.Orange;
            atmoOpticalDistAtlas.SetupForScreenSpaceRendering();
            atmoIrradianceAtlas = new TextureAtlas(this, "AtmosphereIrradianceLUT", ATMO_IRRADIANCE_LUT_PASS, SurfaceFormat.Half4, new AtlasLayoutFixedGrid(IRRADIANCE_LUT_RES * new Int2(1, ATMO_IRRADIANCE_SPLITS + 1), new Int2(CompAtmosphere.MAX_DISPLAYED_COUNT, 1)));
            atmoIrradianceAtlas.Pass.DebugColor = Color.Orange;
            atmoIrradianceAtlas.SetupForScreenSpaceRendering();
            atmoIrradianceAtlas.Pass.RequiredPasses.Add(atmoOpticalDistAtlas.Pass);
            baseMod.MainPass.RequiredPasses.Add(atmoIrradianceAtlas.Pass);
            atmoBakingStates = new List<AtmosphereBakingState>();
        }

        public void StartBakingAtmosphere(CompAtmosphere a)
        {
            // check that the atmosphere is not already baked
            foreach (AtmosphereBakingState state in atmoBakingStates)
                if (a == state.Atmosphere)
                    return;

            // add the atmosphere to the baking pipeline
            AtmosphereBakingState bakingState = new AtmosphereBakingState();
            bakingState.Atmosphere = a;
            atmoBakingStates.Add(bakingState);
        }

        public UpdateType NeededUpdates
        {
            get
            {
                return UpdateType.FrameStart1;
            }
        }

        public CompTextureRef OpticalDistAtlas => atmoOpticalDistAtlas.Texture;

        public CompTextureRef IrradianceAtlas => atmoIrradianceAtlas.Texture;

        public void Update(UpdateType updateType)
        {
            // setup lut baking for new atmospheres
            if (!atmoOpticalDistAtlas.RenderBuffer.LoadingRequired && !atmoIrradianceAtlas.RenderBuffer.LoadingRequired)
            {
                foreach (AtmosphereBakingState bakingState in atmoBakingStates)
                {
                    if (bakingState.Stage != AtmosphereBakingStage.ToBeBaked)
                        continue; // already setup

                    CompAtmosphere a = bakingState.Atmosphere;

                    // allocate and initialize rendering of optical depth lut
                    if (atmoOpticalDistAtlas.Layout.TryAllocateSubTexture((Int2)0, out a.OpticalDistAtlasRegion))
                    {
                        bakingState.DistLutMaterial = new CompMtlAtmosphereDepthLUT(atmoOpticalDistAtlas.Pass, a);
                        bakingState.DistLutCamera = atmoOpticalDistAtlas.AddScreenRenderingCamera(a.OpticalDistAtlasRegion, bakingState.DistLutMaterial);

                        // bake light color luts
                        StartBakingLightColor(a);

                        // initialize rendering of irradiance lut, pre-baking samples divided by zenith angle
                        bakingState.IrradianceRenderBuffer = new CompRenderBuffer(bakingState.Atmosphere, SurfaceFormat.Half4, IRRADIANCE_LUT_RES, IRRADIANCE_LUT_RES * ATMO_IRRADIANCE_SPLITS);
                        CompBakerScreenSpace irradianceBaker = new CompBakerScreenSpace(bakingState.Atmosphere, bakingState.IrradianceRenderBuffer);
                        irradianceBaker.Baker.FinalPass.DebugColor = Color.Orange;
                        irradianceBaker.Baker.FinalPass.Name = "AtmoBakedIrradiance" + a.ID;
                        irradianceBaker.Baker.FinalPass.RequiredPasses.Add(atmoOpticalDistAtlas.Pass);
                        irradianceBaker.Material = new CompMtlAtmosphereIrradianceLUTCache(irradianceBaker, a, atmoOpticalDistAtlas.Texture);
                        irradianceBaker.Baker.OnCompletion = targets =>
                        {
                            // move irradiance lut to the atlas, precalculating an additional quadrant with the total irradiance
                            if (atmoIrradianceAtlas.Layout.TryAllocateSubTexture((Int2)0, out a.IrradianceAtlasRegion))
                            {
                                bakingState.IrradianceLutMaterial = new CompMtlAtmosphereIrradianceLUT(atmoIrradianceAtlas.Pass, targets[0]);
                                bakingState.IrradianceLutCamera = atmoIrradianceAtlas.AddScreenRenderingCamera(a.IrradianceAtlasRegion, bakingState.IrradianceLutMaterial);
                            }
                        };

                        bakingState.Stage = AtmosphereBakingStage.Baking;
                    }
                }
            }

            // disable baking for already rendered luts
            foreach (AtmosphereBakingState state in atmoBakingStates)
            {
                if (state.Stage == AtmosphereBakingStage.ToBeBaked || state.Stage == AtmosphereBakingStage.BakingCompleted)
                    continue; // no baking in progress

                if (state.DistLutCamera != null && state.DistLutCamera.Stats.DrawCallCount > 0)
                {
                    atmoOpticalDistAtlas.Pass.CameraList.Remove(state.DistLutCamera);
                    state.DistLutCamera.Dispose();
                    state.DistLutCamera = null;
                    state.Stage = (AtmosphereBakingStage)((int)state.Stage + 1); // next stage
                }

                if (state.IrradianceLutCamera != null && state.IrradianceLutCamera.Stats.DrawCallCount > 0)
                {
                    atmoIrradianceAtlas.Pass.CameraList.Remove(state.IrradianceLutCamera);
                    state.IrradianceRenderBuffer.Dispose();
                    state.IrradianceRenderBuffer = null;
                    state.IrradianceLutCamera.Dispose();
                    state.IrradianceLutCamera = null;
                    state.Stage = (AtmosphereBakingStage)((int)state.Stage + 1); // next stage
                }
            }

        } // Update()

        /// <summary>
        /// Bakes light color luts.
        /// </summary>
        public void StartBakingLightColor(CompAtmosphere atmosphere)
        {
            // initialize rendering of the light color lut
            Int2 lightColorLutResolution = new Int2(64, 16);
            CompRenderBuffer lightColorBuffer = new CompRenderBuffer(this, SurfaceFormat.Color, lightColorLutResolution.X, lightColorLutResolution.Y);
            CompBakerScreenSpace lightColorBaker = new CompBakerScreenSpace(this, lightColorBuffer);
            lightColorBaker.Material = new CompMtlAtmosphereLightColorLUT(lightColorBaker, atmosphere, atmoOpticalDistAtlas.Texture);
            lightColorBaker.Baker.FinalPass.RequiredPasses.Add(atmoOpticalDistAtlas.Pass);
            lightColorBaker.Baker.FinalPass.Name = "AtmosphereLightColorLUTBaking";
            lightColorBaker.Baker.FinalPass.DebugColor = Color.Orange;
            lightColorBaker.Baker.OnCompletion = targets =>
            {
                // copy the light color lut to a texture
                atmosphere.LightColorGpuLUT.SetSource(targets[0], TexRefFlags.None, true);

                // save a snapshot of the light color lut to be used cpu-side
                targets[0].GetValue().SaveSnapshot();

                // wait for the snapshot to be ready...
                CompEventRtSnapshotReady snapshotReady = new CompEventRtSnapshotReady(lightColorBaker, targets[0].GetValue());
                new CompActionOnEvent(snapshotReady.Event, () =>
                {
                    // data is ready! prepare a LUT to sample the light color
                    atmosphere.LightColorLUT = new LookupTable<Byte4>(lightColorLutResolution.Width, lightColorLutResolution.Height, Byte4.Lerp);
                    targets[0].GetValue().GetSnapshotData<Byte4>(atmosphere.LightColorLUT.Buffer);

                    // use the lut to modulate the light color
                    CompAtmoLightFilter atmoFilter = atmosphere.LightSource.GetFirstChild<CompAtmoLightFilter>();
                    if (atmoFilter == null)
                    {
                        // add a filtering component that will take atmospheric scattering into account
                        atmoFilter = new CompAtmoLightFilter(atmosphere.LightSource, atmosphere.LightSource.LightColor);
                    }
                    atmoFilter.Atmospheres.Add(atmosphere);

                    snapshotReady.Dispose();
                    lightColorBuffer.Dispose();
                });
            }; // end light color baker ready callback
        }
    }
}

using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Terrain;
using System;

namespace Dragonfly.Engine.Test.GraphicTests
{
    public class PlanetTest : GraphicsTest
    {
        private const float PLANET_RADIUS = 6400_000.0f;
        private const int TESSELLATION = 16;

        private CompPlanet planet;
        CompTerrainLODUpdater terrainLODUpdater;
        private CompUiCtrlCheckbox lodCheck, wireframeCheck;

        public PlanetTest()
        {
            Name = "Component Tests: Planet Component";
            EngineUsage = BaseMod.Usage.Generic3D;
        }

        public override void CreateScene()
        {
            Component root = Context.Scene.Root;
            BaseMod baseMod = Context.GetModule<BaseMod>();
            baseMod.Settings.Shadows.MaxShadowDistance = 20000;
            baseMod.Settings.Shadows.MaxOccluderDistance = 40000;
            baseMod.Settings.Shadows.CascadePerFrameCount = 2;
            baseMod.PostProcess.ExposureValue = ExposureHelper.EVClody;
            CompRenderPass mainPass = Context.GetModule<BaseMod>().MainPass;
            mainPass.ClearValue = Float4.Zero;

            AddDebugInfoWindow();

            Float3 xDir = new Float3(1.0f, 0, 0).Normal();
            Float3 yDir = new Float3(0, 0, 1.0f).Normal();//new Float3(0, -1.0f, 0).Normal();

            // add a camera
            Float3 camPos = PLANET_RADIUS * new Float3(0.0f, 1.0f, 2.0f);
            CompTransformEditorMovement cameraController = new CompTransformEditorMovement(root, camPos, Float3.Zero, 2.0f);
            cameraController.Movement.SpeedMps.Set(new CompCumulativeMouseWheel(cameraController, 120000.0f)); // camera speed can be changed with the mouse wheel
            cameraController.UpVector.Set(new CompPlanetUpVector(cameraController, Float3.UnitY)); // up vector will react to a near planet gravity
            mainPass.Camera = new CompCamPerspective(cameraController) { FarPlane = float.PositiveInfinity };

            // add lights
            CompTransformStack sunRot = new CompTransformStack(root);
            sunRot.PushRotationY(new CompTimeSeconds(sunRot), 2.0f);
            CompLightDirectional sun = new CompLightDirectional(CompTransformStack.FromDirection(root, new Float3(1.0f, -0.5f, 1.0f)), new Float3("#ffffff"), ExposureHelper.LuxAtSunset);
            sun.CastShadow = true;
            baseMod.PostProcess.Flares.Lights.Add(sun);

            // add terrain component to be tested
            { 
                CompFractalDataSource terrainData = new CompFractalDataSource(root, (Int2)256, TESSELLATION);

                // use a distance based lod, which use an estimation of the future position to update the terrain
                Component<TiledFloat3> lodPosition = new CompFutureWorldPosition(cameraController, cameraController.Movement.Position, 2.0f * terrainData.Source.MinLodSwitchTimeSeconds);
                DistanceLOD terrainLOD = new DistanceLOD(lodPosition);
                terrainLOD.DensityModifiers.Add(new DistanceLODHeightModifier()); // improve density on slopes
                terrainLODUpdater = new CompTerrainLODUpdater(root, terrainLOD);

                // material factory
                TerrainPhysicalMaterialFactory mfactory = new TerrainPhysicalMaterialFactory(terrainData);
                mfactory.SetDetail("mossgravel1");

                // create the planet
                PlanetParams planetParams = new PlanetParams()
                {
                    LodUpdater = terrainLODUpdater,
                    DataSource = terrainData.Source,
                    MaterialFactory = mfactory,
                    Center = TiledFloat3.Zero,
                };

                int planetSeed = Environment.TickCount;
                PlanetSeed.ExplicitWithRadius(PLANET_RADIUS, planetSeed).ApplyTo(ref planetParams, terrainData);
                planet = new CompPlanet(root, planetParams);
                planet.Atmosphere.LightSource = sun;
                planet.Atmosphere.Visible = true;
                mfactory.AtmosphereForRadiance = planet.Atmosphere;
            }

            // add UI with test commands
            CompUiWindow testWnd = new CompUiWindow(baseMod.UiContainer, "300 150", UiPositioning.Below(TestResults.Window, "10px"));
            testWnd.Title = this.Name;
            {
                CompUiCtrlLabel wireframeLabel = new CompUiCtrlLabel(testWnd, "Wireframe mode:");
                wireframeCheck = new CompUiCtrlCheckbox(testWnd, "0");
                new CompActionOnEvent(wireframeCheck.CheckedChanged, ToggleWireframe);

                CompUiCtrlLabel lodLabel = new CompUiCtrlLabel(testWnd, "LOD Updates:");
                lodCheck = new CompUiCtrlCheckbox(testWnd, "0", true);
                new CompActionOnEvent(lodCheck.CheckedChanged, ToggleLODUpdates);

                // Position all controls
                UiGridLayout layout = new UiGridLayout(testWnd, 10, 3, UiPositioning.Inside(testWnd, "0em 1em"));
                layout.SetRowHeight("2em");
                layout.SetColumnWidth(0, "9em");
                layout.SetColumnWidth(1, "11em");
                layout.SetColumnWidth(2, "5em");
                layout[0, 0] = wireframeLabel;
                layout[0, 1] = wireframeCheck;
                layout[1, 0] = lodLabel;
                layout[1, 1] = lodCheck;
                layout.Apply();
            }
            testWnd.Show();

            // add background audio
            CompAudio windBg = new CompAudio(root, "audio/windy_loop_1.wav");
            windBg.Effects.Add(new CompAudioFxDirGradient(windBg, Float3.Zero, -40.0f, Float3.UnitY * 1200.0f, 0));
            windBg.Effects.Add(new CompAudioFxVolumeRnd(windBg, 0.4f, -12.0f, 0.0f));
            windBg.PlayLoop();
        }

        private void ToggleWireframe()
        {
            for (int i = 0; i < planet.Terrains.Count; i++)
            {
                planet.Terrains[i].WireframeModeEnabled = wireframeCheck.Checked;
            }
        }

        private void ToggleLODUpdates()
        {
            terrainLODUpdater.FreezeLOD = !lodCheck.Checked;
        }
    }
}

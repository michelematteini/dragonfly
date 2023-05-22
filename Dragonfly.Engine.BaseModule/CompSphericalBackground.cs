using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    public class CompSphericalBackground : Component
    {
        private CompMesh bgMesh;
        private CompTextureRef bgRadiance;
        private BackgroundMaterial bgMaterial;

        public CompSphericalBackground(Component parent, string bakedRadiancePath) : this(parent)
        {
            bgRadiance.SetSource(bakedRadiancePath);
        }

        /// <summary>
        /// Create a spherical background from an equirectangular texture file.
        /// </summary>
        /// <param name="equirectTexturePath">The path to an equirectangular texture file. Both HDR and ldr file are accepted.</param>
        /// <param name="cubeEdgeResolution">The resolution of the cube2d on which the input texture will be baked.</param>
        /// <param name="rotationRadiants">An horizontal rotation that can be applied to the background.</param>
        /// <param name="exposureMul">An exposure multiplier that will be applied to the background (in linear space).</param>
        public CompSphericalBackground(Component parent, string equirectTexturePath, int cubeEdgeResolution, float rotationRadiants = 0.0f, float exposureMul = 1.0f) : this(parent)
        {
            // bake the input equirect texture to a radiance map for the background
            CompBakerEquirectToCube2D radianceBaker = new CompBakerEquirectToCube2D(this, cubeEdgeResolution, rotationRadiants, exposureMul);
            radianceBaker.InputEnviromentMap.SetSource(equirectTexturePath);
            radianceBaker.Baker.OnCompletion = cubeRadianceRef =>
            {
                bgRadiance.SetSource(cubeRadianceRef[0], TexRefFlags.HdrColor, true);
                CompActionOnChange compTextureLoaded = new CompActionOnChange(radianceBaker, (c) =>
                {
                    if (bgRadiance.Loaded) 
                        radianceBaker.Dispose();
                });
                compTextureLoaded.Monitored.Add(bgRadiance);
            };
        }

        private CompSphericalBackground(Component parent) : base(parent)
        {
            BaseMod baseMod = Context.GetModule<BaseMod>();
            bgRadiance = new CompTextureRef(this, ColorEncoding.EncodeHdr(Float3.One, RGBE.Encoder));

            // setup background mesh
            bgMaterial = new BackgroundMaterial(this, bgRadiance, 0);
            bgMaterial.DisplayIn(baseMod.MainPass);
            bgMesh = BaseMod.CreateScreenMesh(this);
            bgMesh.Materials.Add(bgMaterial);
        }

        /// <summary>
        /// The level of detail of the baked radiance to be used as background.
        /// </summary>
        public float RadianceLod
        {
            get { return bgMaterial.RadianceLod; }
            set { bgMaterial.RadianceLod = value; }
        }


        private class BackgroundMaterial : CompMaterial
        {
            private float curLod;

            public BackgroundMaterial(Component parent, CompTextureRef backgroundRadiance, float lod) : base(parent) 
            {
                RadianceLod = lod;
                BackgroundRadiance = backgroundRadiance;
                MonitoredParams.Add(BackgroundRadiance);
            }

            public CompTextureRef BackgroundRadiance { get; private set; }


            public float RadianceLod
            {
                get { return curLod; }
                set
                {
                    curLod = value;
                    InvalidateParams();
                }
            }

            public override string EffectName { get { return "Cube2DBg"; } }

            protected override void UpdateParams()
            {
                Shader.SetParam("backgroundTex", BackgroundRadiance);
                Shader.SetParam("backgroundLod", curLod);
            }
        }

    }


}

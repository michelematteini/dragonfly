using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A light created from a cube2d radiance hdr image.
    /// </summary>
    public class CompLightHDRI : Component
    {
        public static CompLightHDRI FromEquirect(Component parent, string equirectTexturePath, int resolution, float exposureMul = 1.0f, float rotationRadiants = 0.0f)
        {
            CompLightHDRI hdriLight = new CompLightHDRI(parent);

            // prepare baking pipeline
            CompBakerEquirectToCube2D cubeBaker = new CompBakerEquirectToCube2D(hdriLight, resolution, rotationRadiants, exposureMul);
            cubeBaker.Baker.OnCompletion = partialResult =>
            {
                // start mipmap baking 
                CompBakerCube2DMipmaps cubeMipmapsBaker = new CompBakerCube2DMipmaps(cubeBaker, cubeBaker.Baker.FinalPass.RenderBuffer, 7);
                cubeMipmapsBaker.Baker.OnCompletion = result =>
                {
                    // save a copy to texture and delete all the created nodes
                    hdriLight.RadianceMap.SetSource(result[0], TexRefFlags.HdrColor, true);
                    CompActionOnChange compTextureLoaded = new CompActionOnChange(cubeBaker, (c) =>
                    {
                        if (hdriLight.RadianceMap.Loaded)
                            cubeBaker.Dispose();
                    });
                    compTextureLoaded.Monitored.Add(hdriLight.RadianceMap);
                };
            };

            // start the baking process
            cubeBaker.InputEnviromentMap.SetSource(equirectTexturePath);

            return hdriLight;
        }

        public CompLightHDRI(Component parent) : base(parent)
        {
            RadianceMap = new CompTextureRef(this, ColorEncoding.EncodeHdr(Color.Black.ToFloat3(), RGBE.Encoder));
        }

        public CompLightHDRI(Component parent, string radianceMapPath) : this(parent)
        {
            RadianceMap.SetSource(radianceMapPath);
        }

        public CompTextureRef RadianceMap { get; private set; }

        /// <summary>
        /// If true, this light is only used if the object to be rendered is contained in its volume.
        /// <para/> If false, this represents a default fallback indirect radiance light.
        /// </summary>
        public bool IsUsageBounded { get; set; }
    }
}
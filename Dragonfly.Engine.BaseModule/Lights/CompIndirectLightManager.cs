using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    public class CompIndirectLightManager : Component, ICompUpdatable
    {
        private CompLightHDRI defaultBackgroundLight;
        private CompTextureRef placeholderBackgroundRadiance;

        public CompIndirectLightManager(Component parent) : base(parent)
        {
            placeholderBackgroundRadiance = new CompTextureRef(this, ColorEncoding.EncodeHdr(Float3.Zero, RGBE.Encoder));
        }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        public void Update(UpdateType updateType)
        {
            // search for a default background radiance 
            IReadOnlyList<CompLightHDRI> hdriLights = GetComponents<CompLightHDRI>();
            for(int i = 0; i < hdriLights.Count; i++)
            {
                if (hdriLights[i].IsUsageBounded) continue; // local probe, skip
                if (!hdriLights[i].Active) continue; // inactive, skip
                defaultBackgroundLight = hdriLights[i];
                break;
            }
        }

        public CompTextureRef DefaultBackgroundRadiance
        {
            get
            {
                if (defaultBackgroundLight == null)
                    return placeholderBackgroundRadiance;

                return defaultBackgroundLight.RadianceMap;
            }   
        }

    }
}

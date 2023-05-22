using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Returns a light modulation color using atmospheres light color lut.
    /// Can be used to modulate the color of a directiona light, scattered by one or multiple atmospheres.
    /// </summary>
    public class CompAtmoLightFilter : Component<Float3>
    {
        private Component<Float3> baseLightColor;
        private BaseMod baseMod;

        public List<CompAtmosphere> Atmospheres { get; private set; }

        public CompAtmoLightFilter(Component owner, Component<Float3> baseLightColor) : base(owner)
        {
            Atmospheres = new List<CompAtmosphere>();
            this.baseLightColor = baseLightColor;
            this.baseMod = Context.GetModule<BaseMod>();
        }

        protected override Float3 getValue()
        {
            Float3 lightColor = baseLightColor.GetValue();

            foreach (CompAtmosphere a in Atmospheres)
            {
                lightColor *= SampleLightColorLUT(a);
            }
            return lightColor;
        }

        private const float ATMO_LCOLOR_SIN_GAMMA = 0.2f; // this encodind param has to match the one in AtmosphericScattering.dfx
        private Float3 SampleLightColorLUT(CompAtmosphere a)
        {
            Float3 hVec = (baseMod.MainPass.Camera.Position - a.Location).ToFloat3();
            float h = hVec.Length;
            float hRelative = h / a.ZeroDensityRadius;
            Float3 hDir = hVec / h;


            // if the atmosphere is too far away, stop filtering both for performance and avoiding artifacts
            if (hRelative > 4.0f)
                return Float3.One;

            // trace the current position to intersect the atmosphere point from which the LUT can be correctly sampled
            if (hRelative > 1.0f)
            {
                Float3 atmoIntesection;
                if (!FMath.RaySphereIntersection(hVec, -a.LightSource.Direction, Float3.Zero, a.ZeroDensityRadius, out atmoIntesection, out _))
                    return Float3.One; // light is not intersecting the atmosphere

                // update sampling position with the intersection
                hVec = atmoIntesection;
                h = hVec.Length;
                hDir = hVec / h;
            }

            // calc LUT coords
            float sinLight = Float3.Dot(hDir, a.LightSource.Direction);
            sinLight = sinLight * (1.0f + ATMO_LCOLOR_SIN_GAMMA) / (Math.Abs(sinLight) + ATMO_LCOLOR_SIN_GAMMA);
            float hNorm = (h - a.MaxDensityRadius) / (a.ZeroDensityRadius - a.MaxDensityRadius);
            Float2 lutCoords = new Float2(0.5f * sinLight + 0.5f, hNorm.Saturate());

            // sample the LUT
            return a.LightColorLUT.SampleBilinear(lutCoords.X, lutCoords.Y).ToFloat3();
        }


    }
}

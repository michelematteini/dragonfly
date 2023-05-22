using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.Terrain
{
    public class MtlModTileCurvature : MaterialModule
    {
        public MtlModTileCurvature(CompMaterial parentMaterial, CompTerrainCurvature curvature, TiledRect3 tileArea) : base(parentMaterial)
        {
            Material.SetVariantValue("curvatureEnabled", !curvature.IsFlat);
            CurvatureLUT = curvature.DisplacementLUT;

            // calc coord range in the curvature lut
            double curvUVMinX, curvUVMinY, curvUVMaxX, curvUVMaxY;
            curvature.TerrainArea.GetCoordsAt(tileArea.Position, out curvUVMinX, out curvUVMinY);
            curvature.TerrainArea.GetCoordsAt(tileArea.EndCorner, out curvUVMaxX, out curvUVMaxY);

            // calc cuvature LUT sampling range in texels
            double texelIndexMax = curvature.Resolution.Y - 1.0;
            double curvTexMinX = curvUVMinX * texelIndexMax;
            double curvTexMinY = curvUVMinY * texelIndexMax;
            double curvTexMaxX = curvUVMaxX * texelIndexMax;
            double curvTexMaxY = curvUVMaxY * texelIndexMax;

            if ((curvTexMaxX - curvTexMinX) > 1.0)
            {
                CurvatureUVAreTexelRelative = false;
                CurvatureTopLeftTexel = Float2.Zero;

                // calc texel snapped coordinates to the curvature LUT
                curvUVMinX = (curvTexMinX + 0.5) / curvature.Resolution.X;
                curvUVMinY = (curvTexMinY + 0.5) / curvature.Resolution.Y;
                curvUVMaxX = (curvTexMaxX + 0.5) / curvature.Resolution.X;
                curvUVMaxY = (curvTexMaxY + 0.5) / curvature.Resolution.Y;
                CurvatureUVScaleOffset = new Float4((float)curvUVMaxX - (float)curvUVMinX, (float)curvUVMaxY - (float)curvUVMinY, (float)curvUVMinX, (float)curvUVMinY);
            }
            else
            {
                // if the texel range is 1 or less, use coordinates relative to that texel (to avoid interpolants precision issues)
                CurvatureUVAreTexelRelative = true;
                CurvatureUVScaleOffset = new Float4((float)curvTexMaxX - (float)curvTexMinX, (float)curvTexMaxY - (float)curvTexMinY, FMath.Frac((float)curvTexMinX), FMath.Frac((float)curvTexMinY));           
                CurvatureTopLeftTexel = new Float2(FMath.Floor((float)curvTexMinX), FMath.Floor((float)curvTexMinY));
                CurvatureTopLeftTexel = (CurvatureTopLeftTexel + 0.5f) / curvature.Resolution; // rescale to sample the center of the texel
            }

            CurvatureXDir = tileArea.XSideDir;
            CurvatureYDir = tileArea.YSideDir;
            CurvatureNormal = tileArea.Normal; 
            CurvatureCenter = curvature.Center;
        }

        public CompTextureRef CurvatureLUT { get; private set; }

        public Float4 CurvatureUVScaleOffset { get; private set; }

        public TiledFloat3 CurvatureCenter { get; private set; }

        public Float3 CurvatureXDir { get; private set; }

        public Float3 CurvatureYDir { get; private set; }

        public Float3 CurvatureNormal { get; private set; }

        public Float2 CurvatureTopLeftTexel { get; private set; }

        public bool CurvatureUVAreTexelRelative { get; private set; }

        protected override void UpdateAdditionalParams(Shader s)
        {
            s.SetParam("curvatureLUT", CurvatureLUT);
            s.SetParam("curvatureUVScaleOffset", CurvatureUVScaleOffset);
            s.SetParam("curvatureXDir", CurvatureXDir);
            s.SetParam("curvatureYDir", CurvatureYDir);
            s.SetParam("curvatureNormal", CurvatureNormal);
            s.SetParam("curvatureTopLeftTexel", CurvatureTopLeftTexel);
            s.SetParam("curvatureUVAreTexelRelative", CurvatureUVAreTexelRelative);
        }
    }
}

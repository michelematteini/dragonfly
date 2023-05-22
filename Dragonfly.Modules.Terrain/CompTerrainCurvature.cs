using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System;

namespace Dragonfly.Terrain
{
    public class CompTerrainCurvature : Component
    {
        private const int CURVATURE_RES = 129;
        private Float2 texelSize;
        private LookupTable<LocalInfo> curvatureLUT;

        internal CompTerrainCurvature(Component parent, TiledRect3 terrainArea, bool isFlat, float curvatureRadius, bool explicitCenter, TiledFloat3 curvatureCenter) : base(parent)
        {
            int resolution;

            if (isFlat)
            {
                // flat terrain
                resolution = 3;
                curvatureCenter = terrainArea.Center; /* needed as a reference for the noise local space in MtlModTerrainDataSrc */
                curvatureRadius = 0;
            }
            else
            {
                // clamp radius to less than 180deg curvature, any less would have to shrink the terrain
                float halfTerrainDiag = terrainArea.Size.CMax() * FMath.SQRT_2 * 0.5f;
                float clampedRadius = Math.Max(curvatureRadius, halfTerrainDiag * 1.001f); 

                if (!explicitCenter)
                {
                    // calc a automatic center from the curvature radius
                    curvatureCenter = terrainArea.Center - terrainArea.Normal * (float)Math.Sqrt((double)clampedRadius * clampedRadius - (double)halfTerrainDiag * halfTerrainDiag);
                }

                resolution = CURVATURE_RES;
            }

            TerrainArea = terrainArea;
            Resolution = new Int2(2 * resolution, resolution);// tiles on the left side, remaining offset on the right
            Radius = curvatureRadius;
            Center = curvatureCenter;
            IsFlat = isFlat;
            texelSize = 1.0f / (Float2)(resolution - 1);
            InitializeDisplacementLUT();
        }

        public Int2 Resolution { get; private set; }


        public TiledRect3 TerrainArea { get; private set; }

        private void InitializeDisplacementLUT()
        {
            // pre-calculate the cpu lut
            curvatureLUT = new LookupTable<LocalInfo>(Resolution.Width / 2, Resolution.Height, LocalInfoLerp);
            Float2 sampleUV;
            int halfWidth = Resolution.Width / 2;
            for (int row = 0; row < Resolution.Height; row++)
            {
                sampleUV.Y = row * texelSize.Y;
                for (int col = 0; col < halfWidth; col++)
                {
                    sampleUV.X = col * texelSize.X;
                    curvatureLUT[col, row] = CalcLocalInfoAtPrecise(TerrainArea.GetPositionAt(sampleUV));
                }
            }

            // create the gpu-side lut
            DisplacementLUT = new CompTextureRef(this, Color.Black);
            TexCreationParams displacementParams = new TexCreationParams();
            displacementParams.Resolution = Resolution;
            displacementParams.Flags = TexRefFlags.None;
            displacementParams.Format = Graphics.SurfaceFormat.Float4;
            displacementParams.TextureInitializer = DisplacementLUTInitializer;
            DisplacementLUT.SetSource(displacementParams);
        }

        private void DisplacementLUTInitializer(Texture destTexture)
        {
            Float4[] displSamples = new Float4[Resolution.Width * Resolution.Height];
            Float2 sampleUV;
            int halfWidth = Resolution.Width / 2;
            for (int row = 0; row < Resolution.Height; row++)
            {
                sampleUV.Y = row * texelSize.Y;
                for (int col = 0; col < halfWidth; col++)
                {
                    sampleUV.X = col * texelSize.X;
                    int iTile = row * Resolution.Width + col;
                    int iOffset = iTile + halfWidth;
                    LocalInfo sample = curvatureLUT[col, row];
                    displSamples[iTile].XYZ = sample.WorldOffset.Tile; // offset tile
                    displSamples[iOffset].XYZ = sample.WorldOffset.Value; // offset value
                    
                    // normal, stored as the slopes in the two tangent directions
                    Float3 slopes = sample.Normal / Math.Max(0.0001f, sample.Normal.Dot(TerrainArea.Normal));
                    displSamples[iTile].W = slopes.Dot(TerrainArea.XSideDir);
                    displSamples[iOffset].W = slopes.Dot(TerrainArea.YSideDir);
                }
            }

            destTexture.SetData<Float4>(displSamples);
        }

        public TiledFloat3 Center { get; private set; }

        public TiledFloat Radius { get; private set; }

        public bool IsFlat { get; private set; }

        public CompTextureRef DisplacementLUT { get; private set; }

        public struct LocalInfo
        {
            public Float3 Normal;
            public TiledFloat3 WorldOffset;
        }

        /// <summary>
        /// Calc curvature info at any world position.
        /// Use the faster CalcLocalInfoAtTilePos() if the position is on a terrain tile.
        /// </summary>
        public LocalInfo CalcLocalInfoAtWorldPos(TiledFloat3 worldPos)
        {
            TiledFloat3 wpFromCenter = worldPos - Center;
            Float3 normal = wpFromCenter.ToFloat3().Normal();
            TiledFloat3 posOnArea;
            if (normal.Dot(TerrainArea.Normal) > 0.1f)
                // world pos is on the same side of the curvature, project the point to the terrain
                posOnArea = TerrainArea.RayPlaneIntersection(worldPos, normal);
            else
                // world pos is on the other side of the curvature, projection is meaningless, just take the closest point instead
                posOnArea = TerrainArea.GetPointClosestTo(worldPos); 
            return CalcLocalInfoAtTilePos(posOnArea);
        }

        /// <summary>
        /// Calc curvature info at a give terrain position. Only works for positions on the terrain area.
        /// </summary>
        public LocalInfo CalcLocalInfoAtTilePos(TiledFloat3 tileWorldPos)
        {
            Float2 sampleUV = TerrainArea.GetCoordsAt(tileWorldPos).Saturate();
            LocalInfo sample = curvatureLUT.SampleBilinear(sampleUV.X, sampleUV.Y);
            sample.Normal = sample.Normal.Normal();
            return sample;
        }

        private static LocalInfo LocalInfoLerp(LocalInfo l1, LocalInfo l2, float amount)
        {
            LocalInfo bilinearInfo;
            bilinearInfo.Normal = l1.Normal.Lerp(l2.Normal, amount);
            bilinearInfo.WorldOffset = TiledFloat3.Lerp(l1.WorldOffset, l2.WorldOffset, amount);
            return bilinearInfo;
        }

        private LocalInfo CalcLocalInfoAtPrecise(TiledFloat3 worldPos)
        {
            LocalInfo result;

            if (IsFlat)
            {
                result.Normal = TerrainArea.Normal;
                result.WorldOffset = TiledFloat3.Zero;
                return result;
            }

            TiledFloat3 wpFromCenter = worldPos - Center;
            result.Normal = wpFromCenter.ToFloat3().Normal();
            result.WorldOffset = Center + result.Normal * Radius - worldPos;

            return result;
        }

    }
}

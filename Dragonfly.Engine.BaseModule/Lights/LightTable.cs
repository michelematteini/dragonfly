using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    internal class LightTable
    {
        public const int LIGHT_STRUCT_SIZE4 = 4;
        public const int SM_STRUCT_SIZE4 = 10;
        public const int LIGHT_TABLE_RECORD_SIZE4 = LIGHT_STRUCT_SIZE4 + SM_STRUCT_SIZE4;
        public const int MAX_LIGHT_COUNT = 1024;

        private enum LightType
        {
            Point = 0,
            Directional = 1,
            Spot = 2
        }

        public CompTextureBuffer Buffer;

        public LightTable(Component parent)
        {
            Buffer = new CompTextureBuffer(parent, new Int2(LIGHT_TABLE_RECORD_SIZE4, MAX_LIGHT_COUNT));
        }

        public int LightCount { get; private set; }

        public int ShadowMapCount { get; private set; }

        public void Reset()
        {
            LightCount = 0;
            ShadowMapCount = 0;
        }

        public void AddLightData(CompLightDirectional l, float smCount, float smFirstIndex)
        {
            if (LightCount < MAX_LIGHT_COUNT)
            {
                int lightOffset = LightCount * LIGHT_TABLE_RECORD_SIZE4;
                Buffer.Values[lightOffset].W = (int)LightType.Directional;
                Buffer.Values[lightOffset + 1].XYZ = l.LightColor.GetValue() * l.Intensity.GetValue();
                Buffer.Values[lightOffset + 2].XYZ = l.Direction;
                Buffer.Values[lightOffset + 3].XY = new Float2(smCount, smFirstIndex);
                LightCount++;
            }
        }

        public void AddLightData(CompLightPoint l, float smCount, float smFirstIndex)
        {
            if (LightCount < MAX_LIGHT_COUNT)
            {
                int lightOffset = LightCount * LIGHT_TABLE_RECORD_SIZE4;
                Buffer.Values[lightOffset].W = (int)LightType.Point;
                Buffer.Values[lightOffset].XYZ = l.Position;
                Buffer.Values[lightOffset + 1].XYZ = l.LightColor.GetValue() * l.Intensity.GetValue();
                Buffer.Values[lightOffset + 3].XY = new Float2(smCount, smFirstIndex);
                LightCount++;
            }
        }

        public void AddLightData(CompLightSpot l, float smCount, float smFirstIndex)
        {
            if (LightCount < MAX_LIGHT_COUNT)
            {
                int lightOffset = LightCount * LIGHT_TABLE_RECORD_SIZE4;
                Buffer.Values[lightOffset].W = (int)LightType.Spot;
                Buffer.Values[lightOffset].XYZ = l.Position;
                Buffer.Values[lightOffset + 1].XYZ = l.LightColor.GetValue() * l.Intensity.GetValue();
                Buffer.Values[lightOffset + 1].W = (float)System.Math.Cos(l.InnerConeAngleRadians * 0.5f);
                Buffer.Values[lightOffset + 2].XYZ = l.Direction;
                Buffer.Values[lightOffset + 2].W = (float)System.Math.Cos(l.OuterConeAngleRadians * 0.5f);
                Buffer.Values[lightOffset + 3].XY = new Float2(smCount, smFirstIndex);
                LightCount++;
            }
        }

        public void AddSerializedShadowState(Float4[] state)
        {
            Array.Copy(state, 0, Buffer.Values, ShadowMapCount * LIGHT_TABLE_RECORD_SIZE4 + LIGHT_STRUCT_SIZE4, Math.Min(SM_STRUCT_SIZE4, state.Length));
            ShadowMapCount++;
        }

        public void UploadValues()
        {
            Buffer.UploadValues();
        }

    }
}

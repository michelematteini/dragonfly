using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Manages a texture buffer containing parameters for a list of atmospheres.
    /// </summary>
    public class CompAtmosphereTable : Component, ICompUpdatable
    {
        public const int ATMO_STRUCT_SIZE4 = 6;

        private BaseMod baseMod;

        public CompAtmosphereTable(Component parent) : base(parent)
        {
            baseMod = Context.GetModule<BaseMod>();
            Buffer = new CompTextureBuffer(this, new Int2(ATMO_STRUCT_SIZE4, CompAtmosphere.MAX_DISPLAYED_COUNT));
            InstanceList = new List<CompAtmosphere>();
        }

        public CompTextureBuffer Buffer { get; private set; }

        public IList<CompAtmosphere> InstanceList { get; private set; }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        public void Update(UpdateType updateType)
        {
            Int3 viewTile = baseMod.CurWorldTile;
            for (int i = 0; i < InstanceList.Count; i++)
            {
                Buffer[0, i] = InstanceList[i].Location.ToFloat3(viewTile).ToFloat4(InstanceList[i].CalcWorldPosBlend(baseMod.MainPass.Camera.Position));
                Buffer[1, i] = new Float4(InstanceList[i].MaxDensityRadius, InstanceList[i].ZeroDensityRadius, InstanceList[i].HeightDensityCoeff, 0.0f);
                Buffer[2, i] = InstanceList[i].LightSource.Direction.ToFloat4(InstanceList[i].MieDirectionalFactor);
                Buffer[3, i] = InstanceList[i].LightIntensity.ToFloat4(0.0f);
                Buffer[4, i] = InstanceList[i].OpticalDistLutScaleOffset;
                Buffer[5, i] = InstanceList[i].IrradianceLutScaleOffset;
            }
            Buffer.UploadValues();
        }
    }
}

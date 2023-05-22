using Dragonfly.Graphics.API.Common;
using Dragonfly.Utils;
using DragonflyGraphicsWrappers.DX11;
using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx11
{
    internal class PSORasterState : ObservableRecord
    {
        public CullModeField CullMode { get; private set; }
        public FillModeField FillMode { get; private set; }

        public PSORasterState()
        {
            CullMode = new CullModeField(this, Graphics.CullMode.CounterClockwise);
            FillMode = new FillModeField(this, Graphics.FillMode.Solid);
        }
    }

    internal class PSORaster : CachedPipelineState<PSORasterState, DF_RasterState, DF_D3D11Device>
    {
        public PSORaster(DF_D3D11Device device) : base(device)
        {
        }

        protected override DF_RasterState CreateState(PSORasterState stateDesc)
        {
            return Device.CreateRasterState(DirectxUtils.CullModeToDX(stateDesc.CullMode), DirectxUtils.FillModeToDX(stateDesc.FillMode));
        }

        protected override IEnumerable<PSORasterState> GenerateAllStateDescriptions()
        {
            PSORasterState rasterDesc = new PSORasterState();

            foreach(CullMode cullMode in Enum.GetValues(typeof(CullMode)))
            {
                rasterDesc.CullMode.Value = cullMode;
                foreach (FillMode fillMode in Enum.GetValues(typeof(FillMode)))
                {
                    if (fillMode == FillMode.Point)
                        continue; // unsupported in dx11
                    rasterDesc.FillMode.Value = fillMode;
                    yield return rasterDesc;
                }
            }
        }

        protected override void ReleaseState(DF_RasterState state)
        {
            state.Release();
        }
    }
}

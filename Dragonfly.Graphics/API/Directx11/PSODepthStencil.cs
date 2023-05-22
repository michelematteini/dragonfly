using Dragonfly.Graphics.API.Common;
using Dragonfly.Utils;
using DragonflyGraphicsWrappers.DX11;
using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx11
{
    internal class PSODepthStencilState : ObservableRecord
    {
        public BoolField DepthEnabled { get; private set; }
        public BoolField DepthWriteEnabled { get; private set; }

        public PSODepthStencilState()
        {
            DepthEnabled = new BoolField(this, true);
            DepthWriteEnabled = new BoolField(this, true);
        }
    }

    internal class PSODepthStencil : CachedPipelineState<PSODepthStencilState, DF_DepthStencilState, DF_D3D11Device>
    {
        public PSODepthStencil(DF_D3D11Device device) : base(device) { }

        protected override DF_DepthStencilState CreateState(PSODepthStencilState stateDesc)
        {
            return Device.CreateDepthStencilState(stateDesc.DepthEnabled, stateDesc.DepthWriteEnabled, DragonflyGraphicsWrappers.DF_CompareFunc.GreaterEqual);
        }

        protected override IEnumerable<PSODepthStencilState> GenerateAllStateDescriptions()
        {
            PSODepthStencilState dsDesc = new PSODepthStencilState();

            for (int enabled = 0; enabled < 2; enabled++)
            {
                dsDesc.DepthEnabled.Value = enabled == 1;
                for (int writeEnabled = 0; writeEnabled < 2; writeEnabled++)
                {
                    dsDesc.DepthWriteEnabled.Value = writeEnabled == 1;
                    yield return dsDesc;
                }
            }
        }

        protected override void ReleaseState(DF_DepthStencilState state)
        {
            state.Release();
        }
    }
}

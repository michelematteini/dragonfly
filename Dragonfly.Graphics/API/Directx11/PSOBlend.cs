using DragonflyGraphicsWrappers.DX11;
using DragonflyGraphicsWrappers;
using System;
using Dragonfly.Utils;
using System.Collections.Generic;
using Dragonfly.Graphics.Resources;
using Dragonfly.Graphics.API.Common;

namespace Dragonfly.Graphics.API.Directx11
{
    internal class PSOBlendState : ObservableRecord
    {
        public BlendModeField Blend { get; private set; }

        public PSOBlendState() 
        {
            Blend = new BlendModeField(this, BlendMode.Opaque);
        }
    }

    internal class PSOBlend : CachedPipelineState<PSOBlendState, DF_BlendState, DF_D3D11Device>
    {
        public PSOBlend(DF_D3D11Device device) : base(device) { }

        protected override DF_BlendState CreateState(PSOBlendState stateDesc)
        {
            return Device.CreateBlendState(stateDesc.Blend != BlendMode.Opaque, DF_BlendMode.SrcAlpha, DF_BlendMode.InvSrcAlpha);
        }

        protected override IEnumerable<PSOBlendState> GenerateAllStateDescriptions()
        {
            PSOBlendState blendState = new PSOBlendState();

            foreach(BlendMode blend in Enum.GetValues(typeof(BlendMode)))
            { 
                blendState.Blend.Value = blend;
                yield return blendState;
            }
        }

        protected override void ReleaseState(DF_BlendState state)
        {
            state.Release();
        }
    }
}

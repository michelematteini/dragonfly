using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Shaders;
using Dragonfly.Utils;
using DragonflyGraphicsWrappers.DX11;
using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx11
{
    internal class PSOSamplerState : ObservableRecord
    {
        public TextureBindingOptionsField Options { get; private set; }

        public PSOSamplerState()
        {
            Options = new TextureBindingOptionsField(this, TextureBindingOptions.None);
        }
    }

    internal class PSOSampler : CachedPipelineState<PSOSamplerState, DF_SamplerState, DF_D3D11Device>
    {
        public PSOSampler(DF_D3D11Device device) : base(device) 
        {
        }

        protected override DF_SamplerState CreateState(PSOSamplerState stateDesc)
        {
            DF_SamplerDesc sampler = new DF_SamplerDesc();

            switch (stateDesc.Options & TextureBindingOptions.Filter)
            {
                case TextureBindingOptions.NoFilter:
                    sampler.Filter = DF_TextureFilterType11.MinMagPointMipLinear;
                    break;
                case TextureBindingOptions.LinearFilter:
                    sampler.Filter = DF_TextureFilterType11.Linear;
                    break;
                case TextureBindingOptions.Anisotropic:
                    sampler.Filter = DF_TextureFilterType11.Anisotropic;
                    break;
            }

            TextureBindingOptions address = stateDesc.Options & TextureBindingOptions.Coords;
            sampler.AddressX = sampler.AddressY = sampler.AddressZ = DirectxUtils.AddressBindToDX(address);
            DirectxUtils.AddressBindToDXBorderColor(address, out sampler.BorderR, out sampler.BorderG, out sampler.BorderB, out sampler.BorderA);

            return Device.CreateSamplerState(sampler);
        }

        protected override IEnumerable<PSOSamplerState> GenerateAllStateDescriptions()
        {
            throw new NotImplementedException();
        }

        protected override void ReleaseState(DF_SamplerState state)
        {
            state.Release();
        }
    }
}

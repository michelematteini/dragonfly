using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Shaders;
using Dragonfly.Utils;
using DragonflyGraphicsWrappers.DX11;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx11
{
    internal struct ShaderCache
    {
        public byte[] CompiledVS;
        public DF_PixelShader11 PS;
        public DF_VertexShader11 VS;
        public int VSStart, VSLen;
    }

    internal class PSOShadersState : ObservableRecord
    {
        public StringField ShaderEffectName { get; private set; }
        public StringField ShaderVariantID { get; private set; }
        public StringField ShaderTemplateName { get; private set; }
        public BoolField Instanced { get; private set; }

        public PSOShadersState()
        {
            ShaderEffectName = new StringField(this, "");
            ShaderVariantID = new StringField(this, "");
            ShaderTemplateName = new StringField(this, "");
            Instanced = new BoolField(this, false);
        }
    }

    internal class PSOShaders : CachedPipelineState<PSOShadersState, ShaderCache, DF_D3D11Device>
    {
        private ShaderBindingTable bindings;
        private IGraphicsAPI api;

        public PSOShaders(DF_D3D11Device device, ShaderBindingTable bindings, IGraphicsAPI api) : base(device)
        {
            this.bindings = bindings;
            this.api = api;
        }

        protected override ShaderCache CreateState(PSOShadersState stateDesc)
        {
            EffectBinding effect = bindings.GetEffect(stateDesc.ShaderEffectName, stateDesc.ShaderTemplateName, stateDesc.ShaderVariantID);
            ShaderCache cacheEntry = new ShaderCache();

            //create VS
            cacheEntry.CompiledVS = bindings.GetProgram(effect.VSName);
            ProgramDB vsPrograms = new ProgramDB(cacheEntry.CompiledVS);
            int vsProgramID = stateDesc.Instanced ? 1 : 0;
            cacheEntry.VSStart = vsPrograms.GetProgramStartID(vsProgramID);
            cacheEntry.VSLen = vsPrograms.GetProgramSize(vsProgramID);
            cacheEntry.VS = Device.CreateVertexShader(cacheEntry.CompiledVS, cacheEntry.VSStart, cacheEntry.VSLen);

            //create PS
            ProgramDB psPrograms = new ProgramDB(bindings.GetProgram(effect.PSName));
            cacheEntry.PS = Device.CreatePixelShader(psPrograms.RawBytes, psPrograms.GetProgramStartID(0), psPrograms.GetProgramSize(0));

            return cacheEntry;
        }

        protected override IEnumerable<PSOShadersState> GenerateAllStateDescriptions()
        {
            throw new System.NotImplementedException();
        }

        protected override void ReleaseState(ShaderCache state)
        {
            state.VS.Release();
            state.PS.Release();
        }

    }
}

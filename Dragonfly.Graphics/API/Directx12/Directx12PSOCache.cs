using Dragonfly.Utils;
using DragonflyGraphicsWrappers.DX12;
using System.Collections.Generic;
using DragonflyGraphicsWrappers;
using Dragonfly.Graphics.Resources;
using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Shaders;

namespace Dragonfly.Graphics.API.Directx12
{
    internal class PSOState : ObservableRecord
    {
        private static int nextPSOID = 0;

        public int ID { get; private set; }

        public VertexTypeField VertexType { get; private set; }
        public StringField ShaderEffectName { get; private set; }
        public StringField ShaderVariantID { get; private set; }
        public StringField ShaderTemplateName { get; private set; }
        public BoolField Instanced { get; private set; }
        public BlendModeField BlendMode { get; private set; }
        public BoolField DepthEnabled { get; private set; }
        public BoolField DepthWriteEnabled { get; private set; }
        public CullModeField CullMode { get; private set; }
        public FillModeField FillMode { get; private set; }
        public IntField RenderTargetCount{ get; private set; }
        public SurfaceFormatField[] RenderTargetFormats { get; private set; }

        public PSOState()
        {
            ID = nextPSOID++;
            VertexType = new VertexTypeField(this, new VertexType());
            ShaderEffectName = new StringField(this, "");
            ShaderVariantID = new StringField(this, "");
            ShaderTemplateName = new StringField(this, "");
            Instanced = new BoolField(this, false);
            BlendMode = new BlendModeField(this, Resources.BlendMode.Opaque);
            DepthEnabled = new BoolField(this, true);
            DepthWriteEnabled = new BoolField(this, true);
            CullMode = new CullModeField(this, Graphics.CullMode.CounterClockwise);
            FillMode = new FillModeField(this, Graphics.FillMode.Solid);
            RenderTargetCount = new IntField(this, 1);
            RenderTargetFormats = new SurfaceFormatField[8];
            for (int i = 0; i < 8; i++)
                RenderTargetFormats[i] = new SurfaceFormatField(this, SurfaceFormat.Color);
        }

        public void Reset()
        {
            VertexType.Value = Graphics.VertexType.Empty;
            ShaderEffectName.Value = string.Empty;
            ShaderVariantID.Value = string.Empty;
            ShaderTemplateName.Value = string.Empty;
            Instanced.Value = false;
            BlendMode.Value = Resources.BlendMode.Opaque;
            DepthEnabled.Value = true;
            DepthWriteEnabled.Value = true;
            CullMode.Value = Graphics.CullMode.CounterClockwise;
            FillMode.Value = Graphics.FillMode.Solid;
            RenderTargetCount.Value = 1;
            for (int i = 0; i < 8; i++)
                RenderTargetFormats[i].Value = SurfaceFormat.Color;
        }
    }

    internal class Directx12PSOCache : CachedPipelineState<PSOState, DF_PipelineState12, DF_D3D12Device>
    {
        private DF_PSODesc12 cachedDesc;
        private ShaderBindingTable bindings;

        public Directx12PSOCache(DF_D3D12Device device, ShaderBindingTable bindings) : base(device)
        {
            this.bindings = bindings; 
            cachedDesc = new DF_PSODesc12();
            cachedDesc.RenderTargetFormats = new DF_SurfaceFormat[8];
        }

        protected override DF_PipelineState12 CreateState(PSOState stateDesc)
        {
            // rasterizer and depth stencil
            {
                cachedDesc.BlendEnable = stateDesc.BlendMode != BlendMode.Opaque;
                switch (stateDesc.BlendMode.Value)
                {
                    case BlendMode.AlphaBlend:
                        cachedDesc.SrcBlend = DF_BlendMode.SrcAlpha;
                        cachedDesc.DestBlend = DF_BlendMode.InvSrcAlpha;
                        break;
                }
                cachedDesc.CullMode = DirectxUtils.CullModeToDX(stateDesc.CullMode);
                cachedDesc.DepthEnabled = stateDesc.DepthEnabled;
                cachedDesc.DepthTest = DF_CompareFunc.GreaterEqual;
                cachedDesc.DepthWriteEnabled = stateDesc.DepthWriteEnabled;
                cachedDesc.FillMode = DirectxUtils.FillModeToDX(stateDesc.FillMode);
            }

            // load shaders
            {
                EffectBinding effect = bindings.GetEffect(stateDesc.ShaderEffectName, stateDesc.ShaderTemplateName, stateDesc.ShaderVariantID);

                // retrieve VS
                cachedDesc.CompiledVS = bindings.GetProgram(effect.VSName);
                ProgramDB vsPrograms = new ProgramDB(cachedDesc.CompiledVS);
                int vsProgramID = stateDesc.Instanced ? 1 : 0;
                cachedDesc.CompiledVSFirstByteIndex = vsPrograms.GetProgramStartID(vsProgramID);
                cachedDesc.CompiledVSByteLength = vsPrograms.GetProgramSize(vsProgramID);

                // retrieve PS
                ProgramDB psPrograms = new ProgramDB(bindings.GetProgram(effect.PSName));
                cachedDesc.CompiledPS = psPrograms.RawBytes;
                cachedDesc.CompiledPSFirstByteIndex = psPrograms.GetProgramStartID(0);
                cachedDesc.CompiledPSByteLength = psPrograms.GetProgramSize(0);

                // in-out layouts
                cachedDesc.InputLayout = DirectxUtils.VertexTypeToDXElems(stateDesc.VertexType);
                cachedDesc.RenderTargetCount = effect.TargetFormats.Length;
                for (int i = 0; i < cachedDesc.RenderTargetCount; i++)
                    cachedDesc.RenderTargetFormats[i] = DirectxUtils.SurfaceFormatToDX(effect.TargetFormats[i]);
            }

            return Device.CreatePSO(cachedDesc);
        }

        protected override IEnumerable<PSOState> GenerateAllStateDescriptions()
        {
            throw new System.NotImplementedException();
        }

        protected override void ReleaseState(DF_PipelineState12 state)
        {
            state.Release();
        }
    }
}

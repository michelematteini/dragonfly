using Dragonfly.Graphics.API.Common;
using Dragonfly.Utils;
using DragonflyGraphicsWrappers.DX11;
using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx11
{
    internal class PSOInputLayoutState : ObservableRecord
    {
        public VertexTypeField VertexType { get; private set; }

        public StringField ShaderEffectName { get; private set; }

        public StringField ShaderVariantID { get; private set; }

        public StringField ShaderTemplateName { get; private set; }

        public BoolField Instanced { get; private set; }

        public PSOInputLayoutState()
        {
            VertexType = new VertexTypeField(this, new VertexType());
            ShaderEffectName = new StringField(this,"");
            ShaderVariantID = new StringField(this, "");
            ShaderTemplateName = new StringField(this, "");
            Instanced = new BoolField(this, false);
        }
    }

    internal class PSOInputLayout : CachedPipelineState<PSOInputLayoutState, DF_InputLayout, DF_D3D11Device>
    {
        private PSOShaders shaderState;

        public PSOInputLayout(DF_D3D11Device device, PSOShaders shaderState) : base(device)
        {
            this.shaderState = shaderState;
        }

        protected override DF_InputLayout CreateState(PSOInputLayoutState stateDesc)
        {
            PSOShadersState shaderDesc = new PSOShadersState();
            shaderDesc.ShaderEffectName.Value = stateDesc.ShaderEffectName;
            shaderDesc.ShaderVariantID.Value = stateDesc.ShaderVariantID;
            shaderDesc.ShaderTemplateName.Value = stateDesc.ShaderTemplateName;
            shaderDesc.Instanced.Value = stateDesc.Instanced;
            ShaderCache curShader = shaderState.GetState(shaderDesc);
            VertexType curVType = stateDesc.VertexType;
            if (stateDesc.Instanced)
                curVType = DirectxUtils.AddInstanceMatrixTo(curVType);

            return Device.CreateInputLayout(DirectxUtils.VertexTypeToDXElems(curVType), curShader.CompiledVS, curShader.VSStart, curShader.VSLen);
        }

        protected override IEnumerable<PSOInputLayoutState> GenerateAllStateDescriptions()
        {
            throw new NotImplementedException();
        }

        protected override void ReleaseState(DF_InputLayout state)
        {
            state.Release();
        }

    }
}

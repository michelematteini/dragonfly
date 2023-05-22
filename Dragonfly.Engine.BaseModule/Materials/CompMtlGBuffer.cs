using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    public class CompMtlGBuffer : CompMaterial
    {
        public enum Type
        {
            TangentSpaceNormalMap = 0,
            Albedo = 1,
            Roughness = 2,
            Displacement = 3,
            Translucency = 4
        }

        private Type matType;

        public CompMtlGBuffer(Component owner, Type materialType, Float3 color, string albedoPath = null, string normalPath = null) : base(owner)
        {
            matType = materialType;
            AlbedoMultiplier = MakeParam(color);
            AlbedoMap = new CompTextureRef(this, Color.White);
            if (!string.IsNullOrEmpty(albedoPath)) AlbedoMap.SetSource(albedoPath);
            MonitoredParams.Add(AlbedoMap);

            NormalMap = new CompTextureRef(this, new Byte4(255, 127, 127, 255));
            if (!string.IsNullOrEmpty(normalPath)) NormalMap.SetSource(normalPath);
            MonitoredParams.Add(NormalMap);
            RoughnessMap = new CompTextureRef(this, Color.White);
            MonitoredParams.Add(RoughnessMap);
            DisplacementMap = new CompTextureRef(this, Color.Black);
            MonitoredParams.Add(DisplacementMap);
            TranslucencyMap = new CompTextureRef(this, Color.Black);
            MonitoredParams.Add(TranslucencyMap);
            AlphaMasking = new MtlModAlphaMasking(this, AlbedoMap);
            CullMode = Context.GetModule<BaseMod>().Settings.DefaultCullMode;
            DoubleSidedNormals = MakeParam(false);

            SetVariantValue("outputType", matType.ToString());
        }

        public override string EffectName
        {
            get
            {
                return "GBuffer";
            }
        }

        public Param<Float3> AlbedoMultiplier { get; private set; }

        public CompTextureRef AlbedoMap { get; protected set; }

        public CompTextureRef NormalMap { get; protected set; }

        public CompTextureRef RoughnessMap { get; protected set; }

        public CompTextureRef DisplacementMap { get; protected set; }

        public CompTextureRef TranslucencyMap { get; protected set; }

        public MtlModAlphaMasking AlphaMasking { get; private set; }

        public Param<bool> DoubleSidedNormals { get; private set; }

        protected override void UpdateParams()
        {
            Shader.SetParam("albedoMap", AlbedoMap);
            Shader.SetParam("albedoMul", AlbedoMultiplier);
            Shader.SetParam("doubleSided", DoubleSidedNormals);

            if (matType == Type.TangentSpaceNormalMap || matType == Type.Displacement)
                Shader.SetParam("normalMap", NormalMap);

            if(matType == Type.Displacement)
                Shader.SetParam("displacementMap", DisplacementMap);

            if(matType == Type.Roughness)
                Shader.SetParam("roughnessMap", RoughnessMap);

            if(matType == Type.Translucency)
                Shader.SetParam("translucencyMap", TranslucencyMap);
        }

        public class Factory : MaterialFactory
        {
            public Factory(Type gBufferType)
            {
                this.GBufferType = gBufferType;
            }

            public bool ClipTransparency { get; set; }

            public Type GBufferType { get; set; }

            protected override CompMaterial CreateMaterialFromDescr(MaterialDescription matDescr, Component parent)
            {
                // default material
                CompMtlGBuffer m = new CompMtlGBuffer(parent, GBufferType, Float3.One);

                // load parameters
                m.AlbedoMultiplier.Value = matDescr.Albedo;

                if (matDescr.HasAlbedoMap)
                    m.AlbedoMap.SetSource(matDescr.AlbedoMapPath);

                if (matDescr.HasNormalMap)
                    m.NormalMap.SetSource(matDescr.NormalMapPath);

                if (matDescr.HasDisplacementMap)
                    m.DisplacementMap.SetSource(matDescr.DisplacementMap);

                if (matDescr.HasRoughnessMap)
                    m.RoughnessMap.SetSource(matDescr.RoughtnessMap);

                if (matDescr.HasTranslucencyMap)
                    m.TranslucencyMap.SetSource(matDescr.TranslucencyMap);

                m.AlphaMasking.Enabled.Value = ClipTransparency || matDescr.ClipTransparency;
                m.DoubleSidedNormals.Value = matDescr.DoubleSided;

                return m;
            }
        }

    }

}

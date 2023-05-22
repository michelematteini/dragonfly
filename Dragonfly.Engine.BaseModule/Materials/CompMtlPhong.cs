using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A material for 3D models, implementing phong lighting model.
    /// </summary>
    public class CompMtlPhong : CompMaterial
    {
        private float specMul, specPower;

        public CompMtlPhong(Component owner, Float3 color, string texturePath, float specularPower, string normalPath) : base(owner)
        {
            DiffuseColor = MakeParam(color);

            DiffuseMap = new CompTextureRef(owner, Color.White);
            MonitoredParams.Add(DiffuseMap);
            if (!string.IsNullOrEmpty(texturePath)) DiffuseMap.SetSource(texturePath);

            NormalMap = new CompTextureRef(owner, new Byte4(255, 127, 127, 255));        
            if (!string.IsNullOrEmpty(normalPath)) NormalMap.SetSource(normalPath);
            MonitoredParams.Add(NormalMap);

            SpecularPower = specularPower;
            AlphaMasking = new MtlModAlphaMasking(this, DiffuseMap);
            AlphaMasking.Enabled.Value = true;
            DoubleSided = MakeParam(false);
            CullMode = Context.GetModule<BaseMod>().Settings.DefaultCullMode;

            Shadows = new MtlModShadowMapBiasing(this);
        }

        public CompMtlPhong(Component owner, Float3 color) : this(owner, color, null, 2.0f, null) { }

        public CompMtlPhong(Component owner, string texturePath) : this(owner, Float3.One, texturePath, 2.0f, null) { }

        public CompMtlPhong(Component owner, string texturePath, string normalPath, float specularPower) : this(owner, Float3.One, texturePath, specularPower, normalPath) { }

        public override string EffectName { get { return "PhongMaterial"; } }

        public Param<Float3> DiffuseColor { get; private set; }

        public float SpecularPower
        {
            get
            {
                return specPower;
            }
            set
            {
                specMul = ((float)System.Math.Log(value, 2.0) / 10.0f).Saturate();
                specMul = specMul * specMul;
                specPower = value;
                InvalidateParams();
            }
        }

        public CompTextureRef DiffuseMap { get; protected set; }

        public CompTextureRef NormalMap { get; protected set; }

        public MtlModAlphaMasking AlphaMasking { get; private set; }

        public Param<bool> DoubleSided { get; private set; }

        public MtlModShadowMapBiasing Shadows { get; private set; }

        protected override void UpdateParams()
        {
            Shader.SetParam("diffuseColor", DiffuseColor);
            Shader.SetParam("totalAmbient", GetTotalAmbient());
            Shader.SetParam("specPower", SpecularPower);
            Shader.SetParam("specMul", specMul);
            Shader.SetParam("diffuseTexture", DiffuseMap);
            Shader.SetParam("normalTexture", NormalMap);
            Shader.SetParam("doubleSided", DoubleSided);
        }

        Float3 GetTotalAmbient()
        {
            Float3 totalAmbient = Float3.Zero;
            foreach(CompLightAmbient ambient in GetComponents<CompLightAmbient>())
                totalAmbient += ambient.Intensity.GetValue() * ambient.LightColor.GetValue();

            return totalAmbient;
        }

        public class Factory : MaterialFactory
        {
            public bool ClipTransparency { get; set; }

            protected override CompMaterial CreateMaterialFromDescr(MaterialDescription matDescr, Component parent)
            {
                // default material
                CompMtlPhong m = new CompMtlPhong(parent, Float3.One);

                // load parameters
                m.DiffuseColor.Value = matDescr.Albedo;
                m.SpecularPower = 1.0f / matDescr.Roughness.Clamp(0.0001f, 1.0f);

                if(matDescr.HasAlbedoMap)
                    m.DiffuseMap.SetSource(matDescr.AlbedoMapPath);

                if (matDescr.HasNormalMap)
                    m.NormalMap.SetSource(matDescr.NormalMapPath);

                m.AlphaMasking.Enabled.Value = ClipTransparency || matDescr.ClipTransparency;
                m.DoubleSided.Value = matDescr.DoubleSided;

                return m;
            }
        }

    }
}

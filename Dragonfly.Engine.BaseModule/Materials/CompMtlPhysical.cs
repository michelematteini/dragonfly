using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A generic photorealistic material using physical parameters to simulare real surfaces.
    /// </summary>
    public class CompMtlPhysical : CompMtlTemplatePhysical
    {

        public CompMtlPhysical(Component owner) : base(owner)
        {
            Albedo = MakeParam((Float3)0.5f);
            AlbedoMap = new CompTextureRef(this, Color.White);
            MonitoredParams.Add(AlbedoMap);
            Roughness = MakeParam(0.5f);
            DoubleSided = MakeParam(false);
            RoughnessMap = new CompTextureRef(this, Color.White);
            MonitoredParams.Add(RoughnessMap);
            NormalMap = new CompTextureRef(owner, new Byte4(255, 127, 127, 255));
            Specular = MakeParam(Float3.One);
            IndexOfRefraction = 1.5f; // dielectric default
            MonitoredParams.Add(NormalMap);
            SpecularMap = new CompTextureRef(this, Color.White);
            MonitoredParams.Add(SpecularMap);
            CompTextureRef displMap = new CompTextureRef(this, Color.Black);
            Displacement = new MtlModDisplacement(this, displMap);
            CullMode = Context.GetModule<BaseMod>().Settings.DefaultCullMode;
            MonitoredParams.Add(GetComponent<CompIndirectLightManager>().DefaultBackgroundRadiance);
            TextureCoords = new MtlModTextureCoords(this);
            AlphaMasking = new MtlModAlphaMasking(this, new CompTextureRef(this, Color.White));
            AlphaMasking.Enabled.Value = true;
            Shadows = new MtlModShadowMapBiasing(this);
        }

        public override string EffectName
        {
            get { return "PhysicalMaterial"; }
        }

        #region Material Params 

        /// <summary>
        /// A multiplier of the color diffused by a surface, in sRGB space.
        /// </summary>
        public Param<Float3> Albedo { get; private set; }

        public CompTextureRef AlbedoMap { get; protected set; }

        public Param<float> Roughness { get; private set; }

        public CompTextureRef RoughnessMap { get; protected set; }

        /// <summary>
        /// If true, the geometry is always considered facing the camera
        /// </summary>
        public Param<bool> DoubleSided { get; private set; }

        /// <summary>
        /// A map containing the detail surface normal.
        /// </summary>
        public CompTextureRef NormalMap { get; protected set; }

        /// <summary>
        /// The ammount of light reflected by the surface at 0 degrees, in sRGB space.
        /// </summary>
        public Param<Float3> Specular { get; private set; }

        /// <summary>
        /// Index of refraction of the material surface, alternative to the specular color.
        /// </summary>
        public float IndexOfRefraction
        {
            get
            {
                float s = FMath.Sqrt(Specular.Value.CMax());
                return (1 + s) / (1 - s);
            }
            set
            {
                float c = Specular.Value.CMax();
                float n = value.Clamp(0.1f, 100.0f);
                float f0 = (1 - n) / (1 + n);
                Specular.Value = (c > 0 ? Specular.Value / c : Float3.One) * f0 * f0;
            }
        }

        /// <summary>
        /// A color texture mapping the Specular parameter over the surface, multiplied to the Specular color. 
        /// </summary>
        public CompTextureRef SpecularMap { get; protected set; }

        /// <summary>
        /// Texture coords modifiers.
        /// </summary>
        public MtlModTextureCoords TextureCoords { get; private set; }

        public MtlModDisplacement Displacement { get; private set; }

        public MtlModAlphaMasking AlphaMasking { get; private set; }

        public MtlModShadowMapBiasing Shadows { get; private set; }

        #endregion

        protected override void UpdateParams()
        {
            base.UpdateParams();
            Shader.SetParam("albedoMul", SRGB.Decode(Albedo));
            Shader.SetParam("albedoMap", AlbedoMap);
            Shader.SetParam("roughMul", Roughness);
            Shader.SetParam("roughMap", RoughnessMap);
            Shader.SetParam("doubleSided", DoubleSided);
            Shader.SetParam("normalMap", NormalMap);
            Shader.SetParam("specular", SRGB.Decode(Specular));
            Shader.SetParam("specularMap", SpecularMap);
            Shader.SetParam("radianceMap", GetComponent<CompIndirectLightManager>().DefaultBackgroundRadiance);
        }

        public class Factory : MaterialFactory
        {
            protected override CompMaterial CreateMaterialFromDescr(MaterialDescription matDescr, Component parent)
            {
                CompMtlPhysical m = new CompMtlPhysical(parent);
                m.Albedo.Value = matDescr.Albedo;
                m.Roughness.Value = m.Roughness;
                m.DoubleSided.Value = matDescr.DoubleSided;

                if (matDescr.HasAlbedoMap)
                    m.AlbedoMap.SetSource(matDescr.AlbedoMapPath);

                if (matDescr.HasRoughnessMap)
                    m.RoughnessMap.SetSource(matDescr.RoughtnessMap);

                if (matDescr.HasNormalMap)
                    m.NormalMap.SetSource(matDescr.NormalMapPath);

                return m;
            }
        }


    }

}

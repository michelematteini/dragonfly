using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A struct containing generic shader parameters that models a BRDF that can be parsed into different compatible CompMaterial.
    /// </summary>
    public struct MaterialDescription
    {
        public Float3 Albedo;
        public string AlbedoMapPath;
        public bool UseTransparencyFromAlbedo;
        /// <summary>
        /// If true, transparency will be treated as a boolean value
        /// </summary>
        public bool ClipTransparency;
        public string NormalMapPath;
        public float Roughness;
        public string RoughtnessMap;
        /// <summary>
        /// If true, the material will be shaded in the side opposite to the surface normal, using an inverted normal vector.
        /// </summary>
        public bool DoubleSided;
        public string DisplacementMap;
        public string TranslucencyMap;

        public static MaterialDescription Default
        {
            get
            {
                MaterialDescription m = new MaterialDescription();
                m.Albedo = Color.White.ToFloat3();
                m.AlbedoMapPath = "";
                m.UseTransparencyFromAlbedo = false;
                m.ClipTransparency = true;
                m.NormalMapPath = "";
                m.Roughness = 0.5f;
                m.RoughtnessMap = "";
                m.DoubleSided = false;
                m.DisplacementMap = "";
                m.TranslucencyMap = "";
                return m;
            }
        }

        public bool HasAlbedoMap
        {
            get { return !string.IsNullOrEmpty(AlbedoMapPath); }
        }

        public bool HasNormalMap
        {
            get { return !string.IsNullOrEmpty(NormalMapPath); }
        }

        public bool HasRoughnessMap
        {
            get { return !string.IsNullOrEmpty(RoughtnessMap); }
        }

        public bool HasDisplacementMap
        {
            get { return !string.IsNullOrEmpty(DisplacementMap); }
        }

        public bool HasTranslucencyMap
        {
            get { return !string.IsNullOrEmpty(TranslucencyMap); }
        }
    }
}

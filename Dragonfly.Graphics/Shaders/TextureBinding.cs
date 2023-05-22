using Dragonfly.Graphics.Math;
using System.IO;

namespace Dragonfly.Graphics.Shaders
{
    public class TextureBinding : InputBinding
    {
        public string TextureType { get; set; }

        public override ShaderBindingType Type { get { return ShaderBindingType.Texture; } }

        public TextureBindingOptions TexBindingOptions { get; set; }

        public bool IsGlobal { get; set; }

        public TextureBinding() : base(string.Empty, string.Empty) { }

        public TextureBinding(string textureName, string textureType, string shaderName) : base(textureName, shaderName)
        {
            TextureType = textureType;
        }

        protected override void LoadAdditionalData(BinaryReader reader)
        {
            base.LoadAdditionalData(reader);
            TextureType = reader.ReadString();
            TexBindingOptions = (TextureBindingOptions)reader.ReadInt32();
            IsGlobal = reader.ReadBoolean();
        }

        protected override void SaveAdditionalData(BinaryWriter writer)
        {
            base.SaveAdditionalData(writer);
            writer.Write(TextureType);
            writer.Write((int)TexBindingOptions);
            writer.Write(IsGlobal);
        }

    }

    public enum TextureBindingOptions
    {
        None = 0,

        /* ADDRESS MODE */

        Wrap =              0 << 0, //default!
        Mirror =            1 << 0,
        Clamp =             2 << 0,
        BorderBlack =       3 << 0,
        BorderWhite =       4 << 0,
        BorderTransparent = 5 << 0,

        /* FILTERING */
        LinearFilter =      0 << 3, //default!
        NoFilter =          1 << 3, 
        Anisotropic =       2 << 3,

        /* MIPMAPS */
        MipMaps =           0 << 5,//default!
        NoMipMaps =         1 << 5,

        /* BINDING */
        ShadingOnly =       0 << 6, //default!
        AlwaysVisible =     2 << 6,
        GeometryOnly =      3 << 6,
        
        /*========== MASKS ============*/
        Coords =            7 << 0,
        Filter =            3 << 3,
        MipMapMode =        1 << 5,
        Visibility =        3 << 6,
        SamplerOptions =    Coords | Filter | MipMapMode
    }

}

using System.IO;

namespace Dragonfly.Graphics.Shaders
{
    public interface IShaderBinding
    {
        ShaderBindingType Type { get; } 

        void Save(BinaryWriter writer);

        void Load(BinaryReader reader);
    }

    public enum ShaderBindingType
    {
        Constant,
        Texture,
        Effect
    }

}

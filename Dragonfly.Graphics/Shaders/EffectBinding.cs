using Dragonfly.Utils;
using System.Collections.Generic;
using System.IO;

namespace Dragonfly.Graphics.Shaders
{
    public class EffectBinding : IShaderBinding
    {
        public string EffectName;
        public string ShaderName;
        public string VSName;
        public string PSName;
        public VertexType InputLayout;
        public Dictionary<string, string> VariantValues;
        public SurfaceFormat[] TargetFormats;
        public bool SupportsInstancing;
        public string Template;

        public ShaderBindingType Type { get { return ShaderBindingType.Effect; } }

        public EffectBinding() { }

        public static EffectBinding FromStream(BinaryReader stream)
        {
            EffectBinding eb = new EffectBinding();
            eb.Load(stream);
            return eb;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(EffectName);
            writer.Write(ShaderName);
            writer.Write(VSName);
            writer.Write(PSName);
            SerializationUtils.WriteCollection(InputLayout.Elements, writer, e => writer.Write((int)e));
            SerializationUtils.WriteDictionary(VariantValues, writer, varValue => writer.Write(varValue));
            SerializationUtils.WriteCollection(TargetFormats, writer, e => writer.Write((int)e));
            writer.Write(SupportsInstancing);
            writer.Write(Template);
        }

        public void Load(BinaryReader reader)
        {
            EffectName = reader.ReadString();
            ShaderName = reader.ReadString();
            VSName = reader.ReadString();
            PSName = reader.ReadString();
            InputLayout = new VertexType(SerializationUtils.ReadCollection(reader, () => (VertexElement)reader.ReadInt32()).ToArray());
            VariantValues = SerializationUtils.ReadDictionary(reader, () => reader.ReadString());
            TargetFormats = SerializationUtils.ReadCollection(reader, () => (SurfaceFormat)reader.ReadInt32()).ToArray();
            SupportsInstancing = reader.ReadBoolean();
            Template = reader.ReadString();
        }
    }
}

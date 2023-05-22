using System.IO;

namespace Dragonfly.Graphics.Shaders
{
    public class ConstantBinding : InputBinding
    {
        public string ConstantType { get; set; }

        public override ShaderBindingType Type { get { return ShaderBindingType.Constant; } }

        public ConstantBinding() : base(string.Empty, string.Empty) { }

        public ConstantBinding(string constantName, string constantType, string shaderName) : base(constantName, shaderName)
        {
            ConstantType = constantType;
        }

        protected override void LoadAdditionalData(BinaryReader reader)
        {
            base.LoadAdditionalData(reader);
            ConstantType = reader.ReadString();
        }

        protected override void SaveAdditionalData(BinaryWriter writer)
        {
            base.SaveAdditionalData(writer);
            writer.Write(ConstantType);
        }
    }
}

using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Shaders
{
    public class ShaderBinding
    {
        public string Name;
        public string ShaderName;
		public string Type;
        public int Index;//index for this binding
        public int BufferIndex;//if this binding is declared inside a buffer, this is the index of the buffer
        public ShaderBindingType BindingType;
		public TextureBindingOptions TexBindingOptions;//used only when binding is of texture type
		public Float3 TextureBorderColor;//used only with TextureBindingOptions.Border

        public ShaderBinding(string shaderName, string name, string type, int index, ShaderBindingType bindType)
            : this(shaderName, name, type, index, -1, bindType)
        {
        }

        public ShaderBinding(string shaderName, string name, string type, int index, int bufferIndex, ShaderBindingType bindType)
        {
            this.ShaderName = shaderName;
            this.Name = name;
			this.Type = type;
            this.Index = index;
            this.BufferIndex = bufferIndex;
            this.BindingType = bindType;
			this.TexBindingOptions = TextureBindingOptions.None;
			this.TextureBorderColor = new Float3(0, 0, 0);
        }

        public ShaderBinding(BinaryReader reader)
        {
            Name = reader.ReadString();
            ShaderName = reader.ReadString();
            Index = reader.ReadInt32();
            BufferIndex = reader.ReadInt32();
            BindingType = (ShaderBindingType)reader.ReadInt32();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(ShaderName);
            writer.Write(Index);
            writer.Write(BufferIndex);
            writer.Write((int)BindingType);
        }
    }

    public enum ShaderBindingType
    {
        Constant,
        Texture
    }
	
	public enum TextureBindingOptions 
	{
		None = 0x0000,
		NoFilter = 0x0100,
		LinearFilter = 0x0000, //default!
		Anisotropic2Filter = 0x0200,
		Anisotropic4Filter = 0x0300,
		Anisotropic8Filter = 0x0400,
		Anisotropic16Filter = 0x0500,
		MipMaps = 0x0000,//default!
		NoMipMaps = 0x1000,
		X_Wrap = 0x0000, //default!
		Y_Wrap = 0x0000, //default!
		Z_Wrap = 0x0000, //default!
		X_Mirror = 0x0001,
		Y_Mirror = 0x0004,
		Z_Mirror = 0x0010,
		X_Clamp = 0x0002,
		Y_Clamp = 0x0008,
		Z_Clamp = 0x0020,
		X_Border = 0x0003,
		Y_Border = 0x000C,
		Z_Border = 0x0030,
		Wrap = 0x0000, //default!
		Mirror = 0x0015,
		Clamp = 0x002A,
		Border = 0x003F,
		
		/*****masks******/
		MipMapMode = 0xF000,
		Filter = 0x0F00,
		Coords = 0x00FF,
		CoordX = 0x0003,
		CoordY = 0x000C,
		CoordZ = 0x0030
	}

}

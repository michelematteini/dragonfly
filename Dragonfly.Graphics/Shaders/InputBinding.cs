using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Shaders
{
    public abstract class InputBinding : IShaderBinding
    {
        public string Name { get; set; }

        public string ShaderName { get; set; }

        /// <summary>
        /// A generic binding address, unused by graphics that can be used by an API without extending this class.
        /// </summary>
        public int Address { get; set; }

        protected InputBinding(string name, string shaderName)
        {
            Name = name;
            ShaderName = shaderName;
        }

        public abstract ShaderBindingType Type { get; }

        public void Load(BinaryReader reader)
        {
            Name = reader.ReadString();
            ShaderName = reader.ReadString();
            Address = reader.ReadInt32();

            LoadAdditionalData(reader);
        }

        protected virtual void LoadAdditionalData(BinaryReader reader) { }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(ShaderName);
            writer.Write(Address);

            SaveAdditionalData(writer);
        }

        protected virtual void SaveAdditionalData(BinaryWriter writer) { }
    }
}


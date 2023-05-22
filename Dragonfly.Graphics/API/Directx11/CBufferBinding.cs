using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Shaders;
using System.Collections.Generic;
using System.IO;

namespace Dragonfly.Graphics.API
{
    internal class CBufferBinding : ConstantBinding
    {
        public const string CBUFFER_CONST_TYPE = "$cbuffer";
        public const string CBUFFER_GLOBAL_PREFIX = "$globals";
        public const string CBUFFER_LOCAL_PREFIX = "$locals";

        public static string CreateName(bool isGlobal, int index)
        {
            return (isGlobal ? CBUFFER_GLOBAL_PREFIX : CBUFFER_LOCAL_PREFIX) + index;
        }

        private Dictionary<string, int> byteAddress;
        private Dictionary<int, int> hashedByteAddress;

        public int ByteSize { get; private set; }

        public CBufferBinding() : this("", false, 0) { }

        public CBufferBinding(string shaderName, bool isGlobal, int index) : base(CreateName(isGlobal, index), CBUFFER_CONST_TYPE, shaderName)
        {
            byteAddress = new Dictionary<string, int>();
            hashedByteAddress = new Dictionary<int, int>();
            ByteSize = 0;
        }

        protected override void LoadAdditionalData(BinaryReader reader)
        {
            base.LoadAdditionalData(reader);

            // check if its a cbuffer ( this binding can also load simple constants )
            if (ConstantType == CBUFFER_CONST_TYPE)
            {
                // load as cbuffer
                ByteSize = reader.ReadInt32();
                int constCount = reader.ReadInt32();
                byteAddress.Clear();
                for (int i = 0; i < constCount; i++)
                {
                    string cname = reader.ReadString();
                    int address = reader.ReadInt32();
                    byteAddress[cname] = address;
                    hashedByteAddress[cname.GetHashCode()] = address;
                }
            }
        }

        protected override void SaveAdditionalData(BinaryWriter writer)
        {
            base.SaveAdditionalData(writer);

            writer.Write(ByteSize);
            writer.Write(byteAddress.Count);

            foreach (string cname in byteAddress.Keys)
            {
                writer.Write(cname);
                writer.Write(byteAddress[cname]);
            }
        }

        public void AddConstant(ShaderSrcFile.ConstantInfo c)
        {
            byteAddress.Add(c.Name, ByteSize);
            ByteSize += DirectxUtils.GetConstRegSize(c) * DirectxUtils.REGISTRY_BYTE_SIZE;
        }

        public int GetByteAddress(string name)
        {
            return byteAddress[name];
        }

        public int GetCRegAddress(string name)
        {
            return byteAddress[name] / DirectxUtils.REGISTRY_BYTE_SIZE;
        }

        public bool HasConstant(string name)
        {
            return byteAddress.ContainsKey(name);
        }

        public int GetByteAddress(int nameHash)
        {
            return hashedByteAddress[nameHash];
        }

        public bool TryGetByteAddress(int nameHash, out int byteAddress)
        {
            return hashedByteAddress.TryGetValue(nameHash, out byteAddress);
        }

        public bool HasConstant(int nameHash)
        {
            return hashedByteAddress.ContainsKey(nameHash);
        }

    }
}

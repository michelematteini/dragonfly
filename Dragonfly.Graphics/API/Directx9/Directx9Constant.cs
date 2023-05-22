using Dragonfly.Graphics.Shaders;
using System;

namespace Dragonfly.Graphics.API.Directx9
{
    struct Directx9Constant
    {
        //Constant string format:
        //[global] <Type> <Name>[<ArraySize>];
        public string Type, Name;
        public bool IsGlobal, IsArray;
        public int ArraySize;
        public string RegisterType; // c = float, i = integer, b = boolean

        public Directx9Constant(string hlsl)
        {
            string[] declElems = hlsl.Trim().Split(new char[] { ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
            IsGlobal = declElems[0] == ShaderPreprocessor.GLOBAL_TOKEN;

            int typeOffset = IsGlobal ? 1 : 0;
            Type = declElems[typeOffset].ToLower();
            Name = declElems[1 + typeOffset];

            IsArray = Name.EndsWith("]");
            ArraySize = 1; // for the only purpose to full-assign the struct
            if (IsArray)
            {
                string[] arrayElems = Name.Split('[', ']');
                ArraySize = int.Parse(arrayElems[1]);
                Name = arrayElems[0];
            }

            RegisterType = Type.StartsWith("int") ? "i" : (Type.StartsWith("bool") ? "b" : "c");
        }

        /// <summary>
        /// Returns the number of registries that are needed to allocate this constant
        /// </summary>
        public int RegisterSize()
        {
            int regSize = 1; // constant size in number of float4 registries

            // in hlsl size can be parsed from the type... (e.g. float3 is 3 DWORDs long)
            string typeSizeStr = string.Empty;
            if (Type.StartsWith("float") && Type.Length > 5) typeSizeStr = Type.Substring(5);
            if (Type.StartsWith("int") && Type.Length > 3) typeSizeStr = Type.Substring(3);
            if (typeSizeStr.Length == 3/* matrix */)
            {
                // matrices row are padded to 1 registry, 
                // registry size is then given by ne num. of columns
                regSize = int.Parse(typeSizeStr[2].ToString());
            }

            // multiply reg number size for the array size to get the length
            // (each array element is alligned to a single registry) 
            if (IsArray) regSize *= ArraySize;

            return regSize;
        }
    }
}

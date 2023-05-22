using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Shaders;
using DragonflyGraphicsWrappers;
using DSLManager.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dragonfly.Graphics.API.Common
{
    internal static class DirectxUtils
    {
        public const int REGISTRY_BYTE_SIZE = 16;

        static DirectxUtils()
        {
            addressMap = new Dictionary<TextureBindingOptions, DF_TextureAddress>();
            addressMap[TextureBindingOptions.Wrap] = DF_TextureAddress.Wrap;
            addressMap[TextureBindingOptions.Clamp] = DF_TextureAddress.Clamp;
            addressMap[TextureBindingOptions.Mirror] = DF_TextureAddress.Mirror;
            addressMap[TextureBindingOptions.BorderBlack] = DF_TextureAddress.Border;
            addressMap[TextureBindingOptions.BorderTransparent] = DF_TextureAddress.Border;
            addressMap[TextureBindingOptions.BorderWhite] = DF_TextureAddress.Border;
        }

        /// <summary>
        /// Return the device registry required by a given shader constant
        /// </summary>
        public static int GetConstRegSize(ShaderSrcFile.ConstantInfo c)
        {
            int regSize = 1; // constant size in number of float4 registries

            // in hlsl size can be parsed from the type... (e.g. float3 is 3 DWORDs long)
            string typeSizeStr = string.Empty;
            if (c.Type.StartsWith("float") && c.Type.Length > 5) typeSizeStr = c.Type.Substring(5);
            if (c.Type.StartsWith("int") && c.Type.Length > 3) typeSizeStr = c.Type.Substring(3);
            if (typeSizeStr.Length == 3/* matrix */)
            {
                // matrices row are padded to 1 registry, 
                // registry size is then given by the num. of columns
                regSize = int.Parse(typeSizeStr[2].ToString());
            }

            // multiply reg number size for the array size to get the length
            // (each array element is alligned to a single registry) 
            if (c.IsArray) regSize *= c.ArraySize;

            return regSize;
        }

        /// <summary>
        /// Create a new vertex type identical to the specified one, with ad additional 4x4 matrix on the instanced stream.
        /// </summary>
        public static VertexType AddInstanceMatrixTo(VertexType vtype)
        {
            VertexElement[] instElems = new VertexElement[vtype.Elements.Length + 4];
            Array.Copy(vtype.Elements, instElems, vtype.Elements.Length);
            instElems[vtype.Elements.Length + 0] = VertexElement.InstanceStream | VertexElement.Float4;
            instElems[vtype.Elements.Length + 1] = VertexElement.InstanceStream | VertexElement.Float4;
            instElems[vtype.Elements.Length + 2] = VertexElement.InstanceStream | VertexElement.Float4;
            instElems[vtype.Elements.Length + 3] = VertexElement.InstanceStream | VertexElement.Float4;
            return new VertexType(instElems);
        }

        public static DF_VertexElement[] VertexTypeToDXElems(VertexType vtype)
        {
            VertexElement[] elems = vtype.Elements;
            DF_VertexElement[] dfElems = new DF_VertexElement[elems.Length];
            byte usageIndex = 0;
            for (int i = 0; i < elems.Length; i++)
            {
                VertexElement eType = (elems[i] & VertexElement.TypeMask);
                VertexElement eStream = (elems[i] & VertexElement.StreamMask);
                bool instanced = eStream == VertexElement.InstanceStream;
                bool isPosition = (eType & VertexElement.AllPositions) == eType;
                dfElems[i].Stream = instanced ? (ushort)1 : (ushort)0;
                dfElems[i].Offset = (ushort)vtype.GetOffset(i);
                dfElems[i].Usage = isPosition ? DF_DeclUsage.Position : DF_DeclUsage.TexCoord;
                dfElems[i].UsageIndex = isPosition ? (byte)0 : usageIndex++;

                switch (eType)
                {
                    case VertexElement.Float:
                        dfElems[i].Type = DF_DeclType.Float;
                        break;
                    case VertexElement.Float2:
                    case VertexElement.Position2:
                        dfElems[i].Type = DF_DeclType.Float2;
                        break;
                    case VertexElement.Float3:
                    case VertexElement.Position3:
                        dfElems[i].Type = DF_DeclType.Float3;
                        break;
                    case VertexElement.Float4:
                    case VertexElement.Position4:
                        dfElems[i].Type = DF_DeclType.Float4;
                        break;
                }
            }

            return dfElems;
        }

        private static Dictionary<TextureBindingOptions, DF_TextureAddress> addressMap;
        public static DF_TextureAddress AddressBindToDX(TextureBindingOptions addressing)
        {
            return addressMap[addressing];
        }

        public static void AddressBindToDXBorderColor(TextureBindingOptions addressing, out float r, out float g, out float b, out float a)
        {
            r = g = b = (addressing == TextureBindingOptions.BorderWhite ? 1.0f : 0.0f);
            a = addressing == TextureBindingOptions.BorderTransparent ? 0.0f : 1.0f;
        }

        public static DF_CullMode CullModeToDX(CullMode value)
        {
            switch (value)
            {
                case CullMode.None:
                    return DF_CullMode.None;

                case CullMode.Clockwise:
                    return DF_CullMode.CullClockwise;

                default: 
                case CullMode.CounterClockwise:
                    return DF_CullMode.CullCounterClockwise;
            }
        }

        public static DF_FillMode FillModeToDX(FillMode value)
        {
            switch (value)
            {
                case FillMode.Point:
                    return DF_FillMode.Point;

                case FillMode.Wireframe:
                    return DF_FillMode.Wireframe;

                default:
                case FillMode.Solid:
                    return DF_FillMode.Solid;
                    
            }
        }

        public static DF_SurfaceFormat SurfaceFormatToDX(SurfaceFormat format)
        {
            switch (format)
            {
                case SurfaceFormat.Color:
                    return DF_SurfaceFormat.A8R8G8B8;
                case SurfaceFormat.Half:
                    return DF_SurfaceFormat.R16F;
                case SurfaceFormat.Half2:
                    return DF_SurfaceFormat.G16R16F;
                case SurfaceFormat.Half4:
                    return DF_SurfaceFormat.A16B16G16R16F;
                case SurfaceFormat.Float:
                    return DF_SurfaceFormat.R32F;
                case SurfaceFormat.Float2:
                    return DF_SurfaceFormat.G32R32F;
                case SurfaceFormat.Float4:
                    return DF_SurfaceFormat.A32B32G32R32F;
                default:
                    return DF_SurfaceFormat.A8R8G8B8;
            }
        }

        private static string ProcessFXCError(string errorStr, string refSourceCode, out int codeLine)
        {
            string[] errors = errorStr.Split('\n');
            codeLine = 0;

            StringBuilder b = new StringBuilder();
            string[] codeLines = null;
            foreach(string error in errors)
            {
                if (error == string.Empty)
                    continue;

                // trim away temp shader reference and add a clean error
                int errorStartID = error.IndexOf("):");
                errorStartID = errorStartID < 0 ? 0 : errorStartID + 3;
                b.Append(error.Substring(errorStartID));

                // parse code line from the error and add the actual statement from the source code
                int eCodeLineID = CodeLineFromFXCError(error);
                string eCodeLine = string.Empty;
                if (eCodeLineID > 0)
                {
                    codeLine = eCodeLineID;
                    if (codeLines == null)
                        codeLines = refSourceCode.Split(new string[] { Environment.NewLine, "\n\r", "\n" }, StringSplitOptions.None);
                    if (codeLines.Length > eCodeLineID)
                        eCodeLine = codeLines[eCodeLineID - 1];
                    b.Append(" - Source Code: ");
                    b.Append(eCodeLine);
                }
                b.AppendLine();
            }

            return b.ToString();
        }

        public static byte[] CompileShader(string sourceCode, string entryPointName, string targetString, bool optimize, bool allowUnboundedTables = false)
        {
            string compilerErrors = "";
            CompileFlags flags = CompileFlags.None;
            if (optimize)
                flags |= CompileFlags.Optimize;
            if (allowUnboundedTables)
                flags |= CompileFlags.EnableUnboundedDescrTables;

            byte[] result = DirectxNativeUtils.CompileShader(sourceCode, entryPointName, targetString, ref compilerErrors, flags);

            // parse errors and warnings
            int eCodeLineID = 0;
            string errorMsg = string.Empty;
            if(!string.IsNullOrEmpty(compilerErrors))
            {
                errorMsg = ProcessFXCError(compilerErrors, sourceCode, out eCodeLineID);
            }

            if (result == null)
            {
#if DEBUG
                File.WriteAllText("LastErrorSource.hlsl", sourceCode);
#endif
                throw new CompileError(eCodeLineID, "FXC012", errorMsg, "");
            }
            else if(!string.IsNullOrEmpty(compilerErrors))
            {
                ConsoleColor curColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(errorMsg);
                Console.ForegroundColor = curColor;
            }

            return result;
        }

        public static string DisassembleShader(byte[] precompiledShader)
        {
            return DirectxNativeUtils.DisassembleShader(precompiledShader);
        }

        public static string DisassembleShader(byte[] precompiledShader, int startIndex, int byteLength)
        {
            return DirectxNativeUtils.DisassembleShader(precompiledShader, startIndex, byteLength);
        }

        public static int CodeLineFromFXCError(string error)
        {
            string[] errorElems = error.Split('(', ')', ',', '-');
            int codeLine = -1;
            if (errorElems.Length >= 2)
            {
                if (!int.TryParse(errorElems[1], out codeLine))
                    return -1;
            }
            return codeLine;
        }

        private static Dictionary<string, string> texelSizeConstNames = new Dictionary<string, string>(); // GetTexelSizeConstantName cache to avoid stressing GC
        public static string GetTexelSizeConstantName(string textureName)
        {
            string constName;
            if (!texelSizeConstNames.TryGetValue(textureName, out constName))
            {
                constName = textureName + "TexelSize";
                texelSizeConstNames[textureName] = constName;
            }
            return constName;
        }

        public static bool IsTexelSizeConstantUsedIn(string srcCode, string textureName)
        {
            return srcCode.Contains(GetTexelSizeConstantName(textureName));
        }

        public static ShaderSrcFile.ConstantInfo CreateTexelSizeConstant(string textureName)
        {
            ShaderSrcFile.ConstantInfo c = new ShaderSrcFile.ConstantInfo();
            c.Type = "float2";
            c.Name = GetTexelSizeConstantName(textureName);
            return c;
        }

        public static List<Int2> DisplayModeToResolutionList(List<DF_DisplayMode> displayModes)
        {
            HashSet<Int2> resolutions = new HashSet<Int2>();

            for (int i = 0; i < displayModes.Count; i++)
                resolutions.Add(new Int2(displayModes[i].Width, displayModes[i].Height));

            return resolutions.ToList();
        }

        public static bool IsFileDDS(byte[] fileData)
        {
            string header = Encoding.ASCII.GetString(fileData, 0, 5);
            return header == "DDS |";
        }

    }
}

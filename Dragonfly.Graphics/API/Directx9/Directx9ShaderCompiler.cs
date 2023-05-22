using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Shaders;
using Dragonfly.Utils;
using System.Collections.Generic;
using System.Text;

namespace Dragonfly.Graphics.API.Directx9
{ 
    internal class Directx9ShaderCompiler : ShaderCompiler
    {
        internal const string GLOBAL_PARAMS_TAG = "$globals";

        private struct RegOffsets
        {
            public int C, I, B;

            public void Reset()
            {
                C = I = B = 0;
            }

            public int GetAndShift(string regType, int regSize)
            {
                // save register and increment pointer to the next free position
                int reg = 0;
                switch (regType)
                {
                    case "c": reg = C; C += regSize; break;
                    case "b": reg = B; B += regSize; break;
                    case "i": reg = I; I += regSize; break;
                }
                return reg;
            }
        }

        private RegOffsets baseLocal; // reg offsets
        private ShaderSrcFile.ConstantInfo instanceMatrix;

        public Directx9ShaderCompiler()
        {
            instanceMatrix = new ShaderSrcFile.ConstantInfo();
            instanceMatrix.Type = "float4x4";
            instanceMatrix.Name = INSTANCE_MATRIX_NAME;
        }

        public override void Initialize()
        {
            baseLocal.Reset();
        }

        public override void SetGlobals(List<ShaderSrcFile.ConstantInfo> globalConsts, List<ShaderSrcFile.TextureInfo> globalTextures, IInputBinder binder)
        {
            // calculate global offsets
            foreach (ShaderSrcFile.ConstantInfo c in globalConsts)
            {
                int regSize = DirectxUtils.GetConstRegSize(c);
                int reg = baseLocal.GetAndShift(GetConstRegType(c), regSize);
                BindConstant(GLOBAL_PARAMS_TAG, binder, c, reg); // bind global with a special shader name
            }
        }

        public override void BindInputs(ShaderSrcFile s, IInputBinder binder)
        {
            // add automatic constants for texel size access
            foreach (ShaderSrcFile.TextureInfo t in s.Textures)
            {
                if (DirectxUtils.IsTexelSizeConstantUsedIn(s.Body, t.Name))
                    s.Constants.Add(DirectxUtils.CreateTexelSizeConstant(t.Name));
            }

            // add (fake) instancing matrix 
            if (s.Body.Contains(INSTANCE_MATRIX_NAME))
                s.Constants.Add(instanceMatrix);

            // bind constants
            RegOffsets nextLocal = baseLocal;
            foreach (ShaderSrcFile.ConstantInfo c in s.Constants)
            {
                if (c.IsGlobal)
                    continue; // already binded

                int regSize = DirectxUtils.GetConstRegSize(c);
                string regType = GetConstRegType(c);
                int reg = nextLocal.GetAndShift(regType, regSize);
                BindConstant(s, binder, c, reg);
            }

            // bind textures
            MoveVSTexturesFirst(s.Textures);
            for (int texAddress = 0; texAddress < s.Textures.Count; texAddress++)
            {
                ShaderSrcFile.TextureInfo t = s.Textures[texAddress];
                TextureBinding texBinding = new TextureBinding(t.Name, t.Type, s.Name);
                texBinding.Address = texAddress;
                texBinding.TexBindingOptions = t.BindingOptions;
                texBinding.IsGlobal = t.IsGlobal;
                binder.BindInput(texBinding);
            }
        }

        /// <summary>
        /// Move the textures used in vertex shader to the first registers (since only the first 4 can be binded to vs)
        /// </summary>
        private void MoveVSTexturesFirst(List<ShaderSrcFile.TextureInfo> textureList)
        {
            for(int i = 0; i < textureList.Count; i++)
            {
                TextureBindingOptions texVis = textureList[i].BindingOptions & TextureBindingOptions.Visibility;
                if (texVis == TextureBindingOptions.GeometryOnly || texVis == TextureBindingOptions.AlwaysVisible)
                {
                    // move textures used in vs first
                    ShaderSrcFile.TextureInfo tex = textureList[i];
                    textureList.RemoveAt(i);
                    textureList.Insert(0, tex);
                }
            }
        }

        private void BindConstant(string shaderName, IInputBinder binder, ShaderSrcFile.ConstantInfo c, int regIndex)
        {
            ConstantBinding constBinding = new ConstantBinding(c.Name, ConvertType(c.Type), shaderName);
            constBinding.Address = regIndex;
            binder.BindInput(constBinding);
        }

        private void BindConstant(ShaderSrcFile shader, IInputBinder binder, ShaderSrcFile.ConstantInfo c, int regIndex)
        {
            BindConstant(shader.Name, binder, c, regIndex);
        }

        public override string ProduceInputDeclarations(ShaderSrcFile s, IBindingTable bindings)
        {
            StringBuilder inputsBlock = new StringBuilder();

            // output constants
            foreach (ShaderSrcFile.ConstantInfo c in s.Constants)
            {
                int address = bindings.GetInput(c.IsGlobal ? GLOBAL_PARAMS_TAG : s.Name, c.Name).Address;
                inputsBlock.AppendFormatLine("{0} {1}{2}: register({4}{3});", ConvertType(c.Type), c.Name, c.IsArray ? ("[" + c.ArraySize + "]") : "", address, GetConstRegType(c));
            }

            // output textures
            foreach (ShaderSrcFile.TextureInfo t in s.Textures)
                inputsBlock.AppendFormatLine("sampler {0}: register(s{1});", t.Name, bindings.GetInput(s.Name, t.Name).Address);

            return inputsBlock.ToString();
        }

        public override byte[] CompileShader(string source, ShaderSrcFile.ProgramInfo programInfo)
        {
            return DirectxUtils.CompileShader(source, programInfo.EntryPoint, GetTargetString(programInfo.Type), OptimizationsEnabled);
        }

        private string GetTargetString(ShaderSrcFile.ProgramType type)
        {
            switch (type)
            {
                case ShaderSrcFile.ProgramType.VertexShader: return "vs_3_0";
                case ShaderSrcFile.ProgramType.PixelShader: return "ps_3_0";
                default: return "";
            }
        }

        public override string ProduceSamplingInstruction(ShaderSrcFile.TextureInfo tex, string texCoordsExpr)
        {
            return "tex2D(" + tex.Name + ", " + texCoordsExpr + ")";
        }

        public override string ProduceSamplingInstruction(ShaderSrcFile.TextureInfo tex, string texCoordsExpr, string lodExpr)
        {
            return "tex2Dlod(" + tex.Name + ", float4(" + texCoordsExpr + ", 0, " + lodExpr + "))";
        }

        private string GetConstRegType(ShaderSrcFile.ConstantInfo c)
        {
            return ConvertType(c.Type).StartsWith("int") ? "i" : (ConvertType(c.Type).StartsWith("bool") ? "b" : "c");
        }

        /// <summary>
        /// Maps constant types to the ones supported in dx9
        /// </summary>
        public string ConvertType(string constType)
        {
#if !DX9_INT_SUPPORT
            // map int to float which are compatible and with better support in SM3.0
            if (constType == "int")
                return "float";
            if (constType == "int3")
                return "float3";
#endif
            return constType;
        }

        public override string ProduceTexelSizeInstruction(ShaderSrcFile.TextureInfo tex)
        {
            return DirectxUtils.GetTexelSizeConstantName(tex.Name);
        }

        public override string ProduceConstModifier()
        {
            return "static const";
        }

        public override string ProduceTextureParamDecl(string paramName)
        {
            return "sampler " + paramName;
        }

        public override string ProduceTextureParamUsage(ShaderSrcFile.TextureInfo texParameter)
        {
            return texParameter.Name;
        }

        public override string GenerateDebugInfo(byte[] compiledProgram)
        {
            return DirectxUtils.DisassembleShader(compiledProgram);
        }

        private string ConvertLayoutType(string dfxType, string semantic)
        {
            if (semantic.ToLower().StartsWith("color"))
            {
                switch (dfxType)
                {
                    case "half2":
                    case "half":
                    case "half4":
                    case "color":
                    case "float":
                    case "float2":
                        return "float4";
                }
            }

            return dfxType;
        }

        public override string ProduceLayoutDecl(ShaderSrcFile.LayoutInfo layout)
        {
            StringBuilder code = new StringBuilder();

            code.AppendLine($"struct {layout.Name}");
            code.AppendLine("{");

            for (int i = 0; i < layout.Elements.Count; i++)
                code.AppendLine($"\t{ConvertLayoutType(layout.Elements[i].Type, layout.Elements[i].SemanticName)} {layout.Elements[i].Name} : {layout.Elements[i].SemanticName};");

            code.AppendLine("};");
            code.AppendLine();

            return code.ToString();
        }
    }

}

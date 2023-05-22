using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Shaders;
using Dragonfly.Utils;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Dragonfly.Graphics.API.Directx11
{
    internal class Directx11ShaderCompiler : ShaderCompiler
    {
        public const string GLOBAL_CB_SHADER_NAME = "$GLOBAL_SHADER";
        private const string INSTANCED_DEFINE_NAME = "INSTANCED";

        private CBufferBinding globalCBuffer;
        private StringBuilder globalCBufferCode;

        public Directx11ShaderCompiler()
        {

        }

        public override void Initialize()
        {
        }

        public override void SetGlobals(List<ShaderSrcFile.ConstantInfo> globalConsts, List<ShaderSrcFile.TextureInfo> globalTextures, IInputBinder binder)
        {
            // prepare global cbuffer
            globalCBuffer = new CBufferBinding(GLOBAL_CB_SHADER_NAME, true, 0);
            globalCBuffer.Address = 0;
            globalCBufferCode = new StringBuilder();
            globalCBufferCode.AppendLine(string.Format("cbuffer Globals : register(b{0})", 0));
            globalCBufferCode.AppendLine("{");

            // add constant declarations
            foreach (ShaderSrcFile.ConstantInfo c in globalConsts)
            {
                globalCBuffer.AddConstant(c);
                globalCBufferCode.AppendLine(string.Format("{0} {1}{2} : packoffset(c{3});", c.Type, c.Name, c.IsArray ? ("[" + c.ArraySize + "]") : "", globalCBuffer.GetCRegAddress(c.Name)));
            }

            // add automatic constants for texel size access, regarless if they're used since we can't check the code for globals
            foreach (ShaderSrcFile.TextureInfo t in globalTextures)
            {
                if (!t.IsGlobal)
                    continue;

                ShaderSrcFile.ConstantInfo c = DirectxUtils.CreateTexelSizeConstant(t.Name);
                globalCBuffer.AddConstant(c);
                globalCBufferCode.AppendLine($"{c.Type} {c.Name} : packoffset(c{globalCBuffer.GetCRegAddress(c.Name)});");
            }

            globalCBufferCode.AppendLine("};");
        }

        public override void PreprocessShaderCode(ref string shaderCode)
        {

        }

        public override byte[] CompileShader(string source, ShaderSrcFile.ProgramInfo programInfo)
        {
            if (programInfo.Type == ShaderSrcFile.ProgramType.VertexShader && programInfo.SupportInstancing)
            {
                // compile a copy without an one with instancing
                byte[] p1 = DirectxUtils.CompileShader(source, programInfo.EntryPoint, GetTargetString(programInfo.Type), OptimizationsEnabled);
                byte[] p2 = DirectxUtils.CompileShader($"#define {INSTANCED_DEFINE_NAME}\n" + source, programInfo.EntryPoint, GetTargetString(programInfo.Type), OptimizationsEnabled);
                return new ProgramDB(new byte[][] { p1, p2 }).RawBytes;
            }
            else
            {
                // compile program
                byte[] p1 = DirectxUtils.CompileShader(source, programInfo.EntryPoint, GetTargetString(programInfo.Type), OptimizationsEnabled);
                return new ProgramDB(new byte[][] { p1 }).RawBytes;
            }
        }

        public override string GenerateDebugInfo(byte[] compiledProgram)
        {
            ProgramDB programs = new ProgramDB(compiledProgram);
            return DirectxUtils.DisassembleShader(compiledProgram, programs.GetProgramStartID(0), programs.GetProgramSize(0));
        }

        public override string ProduceSamplingInstruction(ShaderSrcFile.TextureInfo tex, string texCoordsExpr)
        {
            return string.Format("{0}.Sample( {0}_sampler, {1})", tex.Name, texCoordsExpr);
        }

        public override string ProduceSamplingInstruction(ShaderSrcFile.TextureInfo tex, string texCoordsExpr, string lodExpr)
        {
            return string.Format("{0}.SampleLevel( {0}_sampler, {1}, {2})", tex.Name, texCoordsExpr, lodExpr);
        }


        private string GetTargetString(ShaderSrcFile.ProgramType type)
        {
            switch (type)
            {
                case ShaderSrcFile.ProgramType.VertexShader: return "vs_5_0";
                case ShaderSrcFile.ProgramType.PixelShader: return "ps_5_0";
                default: return "";
            }
        }

        protected override ConstantBinding CreateConstantBinding()
        {
            return new CBufferBinding();
        }

        public override string ProduceTexelSizeInstruction(ShaderSrcFile.TextureInfo tex)
        {
            return DirectxUtils.GetTexelSizeConstantName(tex.Name);
        }

        public override string ProduceConstModifier()
        {
            return "static const";
        }

        public override void BindInputs(ShaderSrcFile s, IInputBinder binder)
        {
            // bind globals (in a shader cbuffer)
            binder.BindInput(globalCBuffer);

            // bind local constants (in a dedicated cbuffer)
            {
                // add automatic constants for texel size access
                foreach (ShaderSrcFile.TextureInfo t in s.Textures)
                {
                    if (t.IsGlobal)
                        continue; 

                    if (DirectxUtils.IsTexelSizeConstantUsedIn(s.Body, t.Name))
                        s.Constants.Add(DirectxUtils.CreateTexelSizeConstant(t.Name));
                }

                // bind local constants
                CBufferBinding localCBuffer = new CBufferBinding(s.Name, false, 0);
                localCBuffer.Address = 1;
                foreach (ShaderSrcFile.ConstantInfo c in s.Constants)
                {
                    ConstantBinding cBinding = new ConstantBinding(c.Name, c.Type, s.Name);
                    cBinding.Address = c.IsGlobal ? 0 : 1;
                    binder.BindInput(cBinding);
                    if (!c.IsGlobal) localCBuffer.AddConstant(c);
                }

                binder.BindInput(localCBuffer);
            }

            // bind textures
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

        public override string ProduceInputDeclarations(ShaderSrcFile s, IBindingTable bindings)
        {
            StringBuilder inputsBlock = new StringBuilder();

            // output global cbuffer code
            inputsBlock.AppendLine(globalCBufferCode.ToString());

            // output local cbuffer
            CBufferBinding localCBuffer = bindings.GetInput(s.Name, CBufferBinding.CreateName(false, 0)) as CBufferBinding;
            inputsBlock.AppendFormatLine("cbuffer {0}Constants{1} : register(b{1})", s.Name, localCBuffer.Address);
            inputsBlock.AppendLine("{");
            foreach (ShaderSrcFile.ConstantInfo c in s.Constants)
            {
                if (c.IsGlobal) continue;
                ConstantBinding cBinding = bindings.GetInput(s.Name, c.Name) as ConstantBinding;
                inputsBlock.AppendFormatLine("{0} {1}{2} : packoffset(c{3});", c.Type, c.Name, c.IsArray ? ("[" + c.ArraySize + "]") : "", localCBuffer.GetCRegAddress(c.Name));
            }
            inputsBlock.AppendLine("};");

            // output texture declarations
            for (int texAddress = 0; texAddress < s.Textures.Count; texAddress++)
            {
                ShaderSrcFile.TextureInfo t = s.Textures[texAddress];
                inputsBlock.AppendFormatLine("Texture2D {0}: register(t{1});", t.Name, texAddress);
                inputsBlock.AppendFormatLine("SamplerState {0}_sampler : register(s{1});", t.Name, texAddress);
            }

            return inputsBlock.ToString();
        }

        public override string ProduceTextureParamDecl(string paramName)
        {
            return string.Format("Texture2D {0}, SamplerState {0}_sampler", paramName);
        }

        public override string ProduceTextureParamUsage(ShaderSrcFile.TextureInfo texParameter)
        {
            return string.Format("{0}, {0}_sampler", texParameter.Name);
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
            int nextTexCoordSlot = 0;
            bool isInputLayout = false;
            for (int i = 0; i < layout.Elements.Count; i++)
            {

                // save the next available texcoord slot
                Match texCoordMatch = Regex.Match(layout.Elements[i].SemanticName, @"TEXCOORD(\d*)");
                if (texCoordMatch.Success && texCoordMatch.Groups.Count > 1)
                {
                    int texCoordID = 0;
                    int.TryParse(texCoordMatch.Groups[1].Value, out texCoordID);
                    nextTexCoordSlot = System.Math.Max(nextTexCoordSlot, texCoordID + 1);
                }

                // rename semantics
                string dx11Semantic = Regex.Replace(layout.Elements[i].SemanticName, @"COLOR(\d+)", "SV_Target$1");
                dx11Semantic = Regex.Replace(dx11Semantic, @"POSITION(\d*)", "SV_Position");

                // infer if this layout is used for mesh vertices based on the presence of a float3 position 
                {
                    Match vertPositionMatch = Regex.Match(layout.Elements[i].SemanticName, @"POSITION(\d*)");
                    isInputLayout |= vertPositionMatch.Success && layout.Elements[i].Type == "float3";
                }

                // output layout element code
                code.AppendLine($"\t{ConvertLayoutType(layout.Elements[i].Type, layout.Elements[i].SemanticName)} {layout.Elements[i].Name} : {dx11Semantic};");
            }

            // conditionally add instancing matrix components
            if(isInputLayout)
            {
                code.AppendLine($"#ifdef {INSTANCED_DEFINE_NAME}");
                code.AppendLine($"\tfloat4 INSTANCE_MAT_C0 : TEXCOORD{nextTexCoordSlot++};");
                code.AppendLine($"\tfloat4 INSTANCE_MAT_C1 : TEXCOORD{nextTexCoordSlot++};");
                code.AppendLine($"\tfloat4 INSTANCE_MAT_C2 : TEXCOORD{nextTexCoordSlot++};");
                code.AppendLine($"\tfloat4 INSTANCE_MAT_C3 : TEXCOORD{nextTexCoordSlot++};");
                code.AppendLine("#endif");
            }

            code.AppendLine("};");
            code.AppendLine();

            return code.ToString();
        }

        public override string ProduceFunctionCode(DFXShaderCompiler.FuncDeclaration f)
        {
            // add instancing matrix preparation code if the function requires it
            if (f.Body.Contains(INSTANCE_MATRIX_NAME))
            {
                StringBuilder code = new StringBuilder();

                // prepare instancing matrix initialization code
                code.AppendLine($"float4x4 {INSTANCE_MATRIX_NAME};");
                code.AppendLine($"#ifdef {INSTANCED_DEFINE_NAME}");
                {
                    // fill the matrix with values from the input assembly
                    for (int col = 0; col < 4; col++)
                        for (int row = 0; row < 4; row++)
                            code.AppendLine($"{INSTANCE_MATRIX_NAME}[{row}][{col}] = {f.ArgNames[0]}.INSTANCE_MAT_C{row}[{col}];");
                }
                code.AppendLine($"#else");
                {
                    // fill the matrix with an identity
                    code.AppendLine($"{INSTANCE_MATRIX_NAME}[0] = float4(1.0, 0.0, 0.0, 0.0);");
                    code.AppendLine($"{INSTANCE_MATRIX_NAME}[1] = float4(0.0, 1.0, 0.0, 0.0);");
                    code.AppendLine($"{INSTANCE_MATRIX_NAME}[2] = float4(0.0, 0.0, 1.0, 0.0);");
                    code.AppendLine($"{INSTANCE_MATRIX_NAME}[3] = float4(0.0, 0.0, 0.0, 1.0);");
                }
                code.AppendLine("#endif");
                code.AppendLine();

                // inject code into the function
                f.Body = code.ToString() + f.Body;
            }

            return base.ProduceFunctionCode(f);
        }

    }
}

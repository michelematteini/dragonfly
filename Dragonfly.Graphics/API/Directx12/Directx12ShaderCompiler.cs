using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Shaders;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Dragonfly.Graphics.API.Directx12
{
    internal class Directx12ShaderCompiler : ShaderCompiler
    {
        public const string GLOBAL_CB_SHADER_NAME = "$GLOBAL_SHADER";
        public const int GLOBAL_CB_ID = 0;
        public const string ROOT_CB_SHADER_NAME = "$DYNAMIC_SHADER";
        public const int ROOT_CB_ID = 2;
        private const string INSTANCED_DEFINE_NAME = "INSTANCED";
        private const string BINDLESS_TEXTURE_ARRAY_NAME = "textures";

        public static CBufferBinding GetGlobalCBFromTable(ShaderBindingTable table)
        {
            return GetGlobalCBFromTable(GLOBAL_CB_SHADER_NAME, GLOBAL_CB_ID, table);
        }

        public static CBufferBinding GetRootCBFromTable(ShaderBindingTable table)
        {
            return GetGlobalCBFromTable(ROOT_CB_SHADER_NAME, ROOT_CB_ID, table);
        }

        private static CBufferBinding GetGlobalCBFromTable(string shaderName, int cbID, ShaderBindingTable table)
        {
            string globalCBufferName = CBufferBinding.CreateName(true, cbID);
            if (!table.ContainsInput(shaderName, globalCBufferName))
                return null;

            return (CBufferBinding)table.GetInput(shaderName, globalCBufferName);
        }

        private StringBuilder sharedInputDeclCode;
        private Directx12StaticSamplers staticSamplers;

        public Directx12ShaderCompiler()
        {
            staticSamplers = new Directx12StaticSamplers();
        }

        public override void Initialize()
        {
            sharedInputDeclCode = new StringBuilder();

            // output static samplers
            for (int i = 0; i < Directx12StaticSamplers.COUNT; i++)
            {
                sharedInputDeclCode.AppendLine($"SamplerState {Directx12StaticSamplers.GetSamplerName(i)} : register(s{i});");
            }
        }

        public override void SetGlobals(List<ShaderSrcFile.ConstantInfo> globalConsts, List<ShaderSrcFile.TextureInfo> globalTextures, IInputBinder binder)
        {
            PrepareGlobalCB(globalConsts, c => !c.IsDynamic, globalTextures, binder, GLOBAL_CB_SHADER_NAME, GLOBAL_CB_ID);
            PrepareGlobalCB(globalConsts, c => c.IsDynamic, new List<ShaderSrcFile.TextureInfo>(), binder, ROOT_CB_SHADER_NAME, ROOT_CB_ID);
        }

        private void PrepareGlobalCB(List<ShaderSrcFile.ConstantInfo> uniforms, Func<ShaderSrcFile.ConstantInfo, bool> uniformFilter, List<ShaderSrcFile.TextureInfo> globalTextures, IInputBinder binder, string shaderName, int address)
        {
            // filter out the needed constants from the list
            List<ShaderSrcFile.ConstantInfo> filteredConsts = new List<ShaderSrcFile.ConstantInfo>();
            foreach (ShaderSrcFile.ConstantInfo c in uniforms)
                if (uniformFilter(c))
                    filteredConsts.Add(c);

            foreach (ShaderSrcFile.TextureInfo t in globalTextures)
            {
                // convert textures to constant ints that will contain their index
                ShaderSrcFile.ConstantInfo texID = TexInfoToIDConst(t);
                filteredConsts.Add(texID);
                // always add a texel size constant for global textures (since if its actually used or not cannot be verified)
                ShaderSrcFile.ConstantInfo texelSizeConst = DirectxUtils.CreateTexelSizeConstant(t.Name);
                filteredConsts.Add(texelSizeConst);
            }

            if (filteredConsts.Count == 0)
                return; // empty cbuffer

            // create cbuffer code and bind its constants
            CBufferBinding cBuffer = new CBufferBinding(shaderName, true, address);
            cBuffer.Address = address;

            sharedInputDeclCode.AppendLine(string.Format("cbuffer Globals{0} : register(b{0})", address));
            sharedInputDeclCode.AppendLine("{");

            foreach (ShaderSrcFile.ConstantInfo c in filteredConsts)
            {
                cBuffer.AddConstant(c);
                sharedInputDeclCode.AppendLine(ProduceInputDeclCode(c, cBuffer));
            }

            sharedInputDeclCode.AppendLine("};");
            binder.BindInput(cBuffer);
        }

        private string ProduceInputDeclCode(ShaderSrcFile.ConstantInfo c, CBufferBinding cBuffer)
        {
            return string.Format("{0} {1}{2} : packoffset(c{3});", c.Type, c.Name, c.IsArray ? ("[" + c.ArraySize + "]") : "", cBuffer.GetCRegAddress(c.Name));
        }

        private ShaderSrcFile.ConstantInfo TexInfoToIDConst(ShaderSrcFile.TextureInfo t)
        {
            return new ShaderSrcFile.ConstantInfo() { IsGlobal = t.IsGlobal, Name = t.Name, Type = "int" };
        }

        public override void PreprocessShaderCode(ref string shaderCode)
        {

        }

        public override byte[] CompileShader(string source, ShaderSrcFile.ProgramInfo programInfo)
        {
            if (programInfo.Type == ShaderSrcFile.ProgramType.VertexShader && programInfo.SupportInstancing)
            {
                // compile a copy without an one with instancing
                byte[] p1 = DirectxUtils.CompileShader(source, programInfo.EntryPoint, GetTargetString(programInfo.Type), OptimizationsEnabled, true);
                byte[] p2 = DirectxUtils.CompileShader($"#define {INSTANCED_DEFINE_NAME}\n" + source, programInfo.EntryPoint, GetTargetString(programInfo.Type), OptimizationsEnabled, true);
                return new ProgramDB(new byte[][] { p1, p2 }).RawBytes;
            }
            else
            {
                // compile program
                byte[] p1 = DirectxUtils.CompileShader(source, programInfo.EntryPoint, GetTargetString(programInfo.Type), OptimizationsEnabled, true);
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
            return $"{BINDLESS_TEXTURE_ARRAY_NAME}[{tex.Name}].Sample({GetSamplerName(tex)}, {texCoordsExpr})";
        }

        public override string ProduceSamplingInstruction(ShaderSrcFile.TextureInfo tex, string texCoordsExpr, string lodExpr)
        {
            return $"{BINDLESS_TEXTURE_ARRAY_NAME}[{tex.Name}].SampleLevel({GetSamplerName(tex)}, {texCoordsExpr}, {lodExpr})";
        }

        public override string ProduceTextureParamDecl(string paramName)
        {
            return string.Format("int {0}, SamplerState {0}_sampler", paramName);
        }

        public override string ProduceTextureParamUsage(ShaderSrcFile.TextureInfo texParameter)
        {
            return $"{texParameter.Name}, {GetSamplerName(texParameter)}";
        }

        private string GetSamplerName(ShaderSrcFile.TextureInfo tex)
        {
            if (tex.IsParameter)
                return tex.Name + "_sampler";

            return staticSamplers.GetSamplerName(tex.BindingOptions & TextureBindingOptions.SamplerOptions);
        }

        private string GetTargetString(ShaderSrcFile.ProgramType type)
        {
            switch (type)
            {
                case ShaderSrcFile.ProgramType.VertexShader: return "vs_5_1";
                case ShaderSrcFile.ProgramType.PixelShader: return "ps_5_1";
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
            // bind local constants (in a dedicated cbuffer)
            {
                // add automatic constants for texel size access
                foreach (ShaderSrcFile.TextureInfo t in s.Textures)
                {
                    if (!t.IsGlobal && DirectxUtils.IsTexelSizeConstantUsedIn(s.Body, t.Name))
                        s.Constants.Add(DirectxUtils.CreateTexelSizeConstant(t.Name));
                }

                // create a local cbuffer that will hold all constants 
                CBufferBinding localCBuffer = new CBufferBinding(s.Name, false, 0);
                localCBuffer.Address = 1;
                
                // bind local constants
                foreach (ShaderSrcFile.ConstantInfo c in s.Constants)
                {
                    if (c.IsGlobal)
                        continue;
                    localCBuffer.AddConstant(c);
                }

                // bind textures as int indices that will allow for bindless referencing
                foreach (ShaderSrcFile.TextureInfo t in s.Textures)
                {
                    if (t.IsGlobal)
                        continue;
                    ShaderSrcFile.ConstantInfo texIDConst = TexInfoToIDConst(t);
                    localCBuffer.AddConstant(texIDConst);
                    s.Constants.Add(texIDConst);

                }

                // save cbuffer to the binding table
                binder.BindInput(localCBuffer);
            }
        }

        public override string ProduceInputDeclarations(ShaderSrcFile s, IBindingTable bindings)
        {
            StringBuilder inputsBlock = new StringBuilder();

            // output shader input declaration code
            inputsBlock.AppendLine(sharedInputDeclCode.ToString());

            // output local cbuffer
            CBufferBinding localCBuffer = bindings.GetInput(s.Name, CBufferBinding.CreateName(false, 0)) as CBufferBinding;
            inputsBlock.AppendFormatLine("cbuffer {0}Constants{1} : register(b{1})", s.Name, localCBuffer.Address);
            inputsBlock.AppendLine("{");
            foreach (ShaderSrcFile.ConstantInfo c in s.Constants)
            {
                if (c.IsGlobal) 
                    continue;
                inputsBlock.AppendFormatLine(ProduceInputDeclCode(c, localCBuffer));
            }
            inputsBlock.AppendLine("};");

            // output bindless texture array declaration
            inputsBlock.AppendLine($"Texture2D {BINDLESS_TEXTURE_ARRAY_NAME}[]: register(t0);");
            
            return inputsBlock.ToString();
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
            if (isInputLayout)
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
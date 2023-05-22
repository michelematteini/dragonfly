using Dragonfly.Utils;
using DSLManager.Generation;
using DSLManager.Languages;
using DSLManager.Parsing;
using DSLManager.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Dragonfly.Graphics.Shaders
{
    public class DFXShaderCompiler : BasicCompiler<object, CodeProject>
    {
        // dfx extension keywords and instrinsics
        private const string TOKEN_MOD_GLOBAL = "global"; // a uniform that should be shared across all shaders
        private const string TOKEN_MOD_CONST = "const"; // not visible from cpu
        private const string TOKEN_MOD_DYNAMIC = "dynamic"; // a uniform that is frequently changing
        private const string TOKEN_VSPROGRAM = "VS";
        private const string TOKEN_PSPROGRAM = "PS";
        private const string TOKEN_INSTANCING_SUPPORT = "instanced";
        private const string TOKEN_TEMPLATE_EFFECT = "template";
        private const string TOKEN_AUTO_VERTEXTYPE = "vertex_t";
        private const string TOKEN_LAYOUT = "layout";
        private const string INSTRINSIC_SAMPLING_FUNC = "sample";
        private const string INSTRINSIC_SAMPLING_LOD_FUNC = "sampleLevel";
        private const string INSTRINSIC_SAMPLING_LOD0_FUNC = "sampleLevel0";
        private const string INSTRINSIC_TEXEL_SIZE_FUNC = "texelSize"; // intrinsic function that returns the texel size of a texture

        private IGraphicsAPI destPlatform;

        // parsing context cache
        private Dictionary<string, ShaderSrcFile.TextureInfo> texDeclarations;
        private HashSet<string> externalTextures;
        private List <ShaderSrcFile.Variant> curVariants;
        private string shaderName;
        private Stack<HashSet<string>> texParamStack;
        private Dictionary<string, FuncDeclaration> funcDeclarations;

        public DFXShaderCompiler(IGraphicsAPI destPlatform)
        {
            this.destPlatform = destPlatform;
            FileExtension = "dfx";
            InlineCommentStart = "//";
            MultilineCommentStart = "/*";
            MultilineCommentEnd = "*/";
            IsMultipassCompiler = true;
            NativeCompiler = destPlatform.CreateShaderCompiler();
            
            /*==== Shader Grammar ====*/

            // Program
            AddRule("Program ::= ShaderDecl, { UniformDeclaration | TexDeclaration | StructDeclaration | LayoutDeclaration | EffectDeclaration | FuncDeclaration | TemplateFuncDeclaration | UsingDirective | PreprocDirective | VariantDeclaration }*", STD_Program);

            // ShaderDecl
            /// defines the name of the shader, together with 'using' create a smart #include that re-sort code and let user also reference other custom extension-declarations that do not make it to the final hlsl code
            AddRule("ShaderDecl ::= ''shader'', '':'', Name, '';''", SDT_ShaderDecl);

            // UsingDirective
            /// see ShaderDecl
            AddRule("UsingDirective ::= ''using'', Name, '';''", SDT_UsingDirective);

            // VariantDeclaration
            /// declare an fake uniform that is 'unrolled' at compile time in different shaders, but can be used as a normal uniform at Shader class level.
            AddRule("VariantDeclaration ::= ''variant'', Name, ['':'', Name, { '','', Name }+], '';''", SDT_VariantDeclaration);

            // PreprocDirective
            RulePriority ppPriority = RulePriority.Default;
            ppPriority.ReducePriority--;
            AddRule("PreprocDirective ::= ''#define'', Name, [VectLiteral | Name]", SDT_PassCodeLine, ppPriority);
            AddRule("PreprocDirective ::= ''#ifdef'', Name", SDT_PassCodeLine);
            AddRule("PreprocDirective ::= ''#ifndef'', Name", SDT_PassCodeLine);
            AddRule("PreprocDirective ::= ''#else''", SDT_PassCodeLine);
            AddRule("PreprocDirective ::= ''#endif''", SDT_PassCodeLine);
            AddRule("PreprocDirective ::= ''#if'', Name, (''=='' | ''!=''), Name", SDT_PassCodeLine);
            AddRule("PreprocDirective ::= ''#elif'', Name, (''=='' | ''!=''), Name", SDT_PassCodeLine);

            // UniformDeclaration
            /// TOKEN_MOD_GLOBAL make the uniform shared among all shaders that import it with the using directive, can then be modified directly from a CommandList, without a shader reference.
            /// TOKEN_MOD_DYNAMIC optimize this uniform for frequent changes (i.e. change count >> once per frame). API dependent.
            AddRule("UniformDeclaration ::= VarModifier, TypeNameBlock, [(VarIndexing, [''='', ArrayInitializer]) | (''='', VectLiteral)], '';''", SDT_UniformDeclaration);
            AddRule($"VarModifier ::= [''{TOKEN_MOD_GLOBAL}'' | ''{TOKEN_MOD_CONST}'' | ''{TOKEN_MOD_DYNAMIC}'' | ''{TOKEN_MOD_GLOBAL} {TOKEN_MOD_DYNAMIC}'']", SDT_PassCode);
            AddRule("ArrayInitializer ::= ''{'', VectLiteral, {'','', VectLiteral}*, ''}''", SDT_PassCode);

            // TexDeclaration
            AddRule("TexDeclaration ::= VarModifier, TextureType, Name, ['':'', SamplerOption, {'','',  SamplerOption}*], '';''", SDT_TexDeclaration);
            AddRule("SamplerOption ::= Name", SDT_PassCode);

            // StructDeclaration
            AddRule("StructDeclaration ::= ''struct'', Name, ''{'', {VarDeclaration, '';''}*, ''}'', '';''", SDT_StructDeclaration);

            // LayoutDeclaration
            /// special struct to declare an input / output layout, with special types that also specify surface formats (allow automatic PSO deduction)
            AddRule("LayoutDeclaration ::= ''"+ TOKEN_LAYOUT + "'', Name, ''{'', {TypeNameBlock, '':'', SemanticName,'';''}*, ''}'', '';''", SDT_LayoutDeclaration);
            AddRule("SemanticName ::= Name", SDT_InstanceValue);

            // EffectDeclaration
            /// define a set of programs to be used to setup the rendering pipeline
            /// Effects can be templates. An effect template is an incomplete / abstract effect that can be used by a specific rendering pass. An effect can implement an effect template to 'support' being used by that pass.
            /// Effects can implement multiple templates. An effect template can be a specialization of another.
            /// Decl 1 - normal effect decl, that can optionally be a template
            AddRule($"EffectDeclaration ::= [''{TOKEN_INSTANCING_SUPPORT}''], [''{TOKEN_TEMPLATE_EFFECT}''], ''effect'', Name, ''{{'', ''VS'', ''='', Name, '','', ''PS'', ''='', Name, ''}}'', '';''", SDT_EffectDeclaration);
            /// Decl 2 - effect that implement one or multiple templates
            AddRule($"EffectDeclaration ::= ''effect'', Name, '':'', Name, ''('', Name, '')'', {{'','', Name, ''('', Name, '')''}}*, '';''", SDT_DerivedEffectDeclaration);
            /// Decl 2 - template specialization
            AddRule($"EffectDeclaration ::= ''{TOKEN_TEMPLATE_EFFECT}'', ''effect'', Name, '':'', Name, '';''", SDT_DerivedTemplateDeclaration);

            // FuncDeclaration
            // angular brackets can enclose a defined type when the function implements a template.
            AddRule($"FuncDeclaration ::= [ShaderProgramType], TypeNameBlock, [''<'', Name, ''>''], ''('', [ArgList], '')'',  CodeBody", SDT_FuncDeclaration);
            AddRule($"ShaderProgramType ::= ''{TOKEN_VSPROGRAM}'' | ''{TOKEN_PSPROGRAM}''", SDT_PassCode);
            AddRule("ArgList ::= [InputModifier], TypeNameBlock, {'','', [InputModifier], TypeNameBlock}*", SDT_ArgList);
            AddRule("InputModifier ::= ''inout'' | ''out''", SDT_PassCode);

            // TemplateFuncDeclaration
            /// An incomplete / abstract function definen inside an effect template, to be implemented.
            /// These functions can forward declare to be using struct types that are not actually defined, and that should be defined by the implementing effect.
            /// The forward declared types can then be used everywhere in the shader without a definition.
            AddRule($"TemplateFuncDeclaration ::= ''{TOKEN_TEMPLATE_EFFECT}'', [''<'', ''struct'', Name, ''>''], TypeNameBlock, ''('', [ArgList], '')'',  '';''", SDT_TemplateFuncDeclaration); // forward declaration

            // CodeBody
            AddRule("CodeBody ::= ''{'', {CodeBodyContent}*, ''}''", SDT_CodeBody);
            AddRule("CodeBodyContent ::= ForBlock | IfBlock | HlslStatement | PreprocDirective", SDT_PassCodeLine);
            AddRule("CodeBodyOptBracket ::= CodeBody | CodeBodyContent", SDT_CodeBodyOptBracket);
            AddRule("ForBlock ::= UnrollType, ''for'', ''('', (VarDeclaration | VarAssignment), '';'', HlslExpr, '';'', IncrExpr, { '','', IncrExpr }*,'')'', CodeBodyOptBracket", SDT_PassCode);
            AddRule("UnrollType ::= ''['', #name, '']''", SDT_PassCode);
            AddRule("IncrExpr ::= VarAssignment", SDT_PassCode);
            AddRule("IncrExpr ::= VarAccess, HlslPrePostOp", SDT_PassCode);
            AddRule("IncrExpr ::= HlslPrePostOp, VarAccess", SDT_PassCode);
            RulePriority ifPriority = RulePriority.ShiftOver(RulePriority.Default);
            AddRule("IfBlock ::= ''if'', ''('', HlslExpr, '')'', CodeBodyOptBracket, { ElifBlock }*, [ ElseBlock ]", SDT_IfBlock, ifPriority);
            AddRule("ElifBlock ::= ''else'', ''if'', ''('', HlslExpr, '')'', CodeBodyOptBracket", SDT_ElifBlock, RulePriority.ShiftOver(RulePriority.Override(ifPriority)));
            AddRule("ElseBlock ::= ''else'', CodeBodyOptBracket", SDT_ElseBlock, RulePriority.Override(ifPriority));

            // Variables shader code rules
            AddRule("VectorType ::= ''float2'' | ''float3'' | ''float4'' | ''float3x3'' | ''float4x4'' | ''int2'' | ''int3'' | ''int4''", SDT_InstanceValue);
            AddRule("TextureType ::= ''texture''", SDT_InstanceValue);
            RulePriority varTypePriority = RulePriority.Default;
            varTypePriority.ReducePriority--; // reduced priority to allow for rules that start with a type to take over
            AddRule("VarType ::= VectorType | ''float'' | ''int'' | ''bool'' | ''void'' | TextureType", SDT_InstanceValue, varTypePriority);
            AddRule("VarDeclaration ::= TypeNameBlock, [VarIndexing], [''='', HlslExpr]", SDT_PassCode);
            AddRule("VarDeclaration ::= VarType, Name, {'','', Name }+", SDT_PassCode);
            AddRule("VarAccess ::= Name, VarAccessor", SDT_PassCode);
            AddRule("VarAccessor ::= {VarIndexing | (''.'', Name)}*", SDT_PassCode);
            AddRule("VarIndexing ::= ''['', HlslExpr, '']''", SDT_VarIndexing);
            AddRule("VarAssignment ::= VarAccess, HlslAssignOp, HlslExpr", SDT_PassCode);
            AddRule("VarAssignment ::= VarAccess, HlslAssignOp, VarAssignment", SDT_PassCode);
            AddRule("Literal ::= (#int | #real | (''.'', #int)), [''f'']", SDT_Literal);
            AddRule("VectLiteral ::= VectorType, ''('', [''-'' | ''+''], Literal, {'','', [''-'' | ''+''], Literal}*, '')''", SDT_PassCode);
            AddRule("VectLiteral ::= Literal", SDT_PassCode);

            AddRule("TypeNameBlock ::= VarType, Name", SDT_TypeNameBlock);
            AddRule("TypeNameBlock ::= Name, Name", SDT_TypeNameBlock);

            // Hlsl statements
            AddRule("HlslStatement ::= (VarDeclaration | VarAssignment | ReturnStatement | FuncCall | ''continue'' | ''break''), '';''", SDT_PassCode);
            AddRule("ReturnStatement ::= ''return'', [HlslExpr]", SDT_PassCode);

            // Hlsl expressions
            RulePriority opPriority1 = RulePriority.ReduceOver(RulePriority.Default);
            RulePriority opPriority2 = RulePriority.ReduceOver(opPriority1);
            RulePriority opPriority3 = RulePriority.ReduceOver(opPriority2);
            AddRule("HlslExpr ::= Literal | VarAccess | (FuncCall, VarAccessor) | (HlslBracketBlock, VarAccessor)", SDT_PassCode);
            AddRule("HlslExpr ::= HlslPrePostOp, HlslExpr", SDT_PassCode, opPriority3);
            AddRule("HlslExpr ::= HlslUnaryOp, HlslExpr", SDT_PassCode, opPriority3);
            AddRule("HlslExpr ::= HlslExpr, HlslPrePostOp", SDT_PassCode, opPriority3);
            AddRule("HlslBracketBlock ::= ''('', HlslExpr, '')''", SDT_PassCode);
            AddRule("HlslExpr ::= ''('', VarType, '')'', HlslExpr", SDT_PassCode, opPriority2);
            AddRule("HlslExpr ::= HlslBracketBlock, Literal", SDT_PassCode); // zero struct initializer
            AddRule("HlslExpr ::= HlslExpr, HlslOp, HlslExpr", SDT_PassCode, opPriority1);
            AddRule("HlslExpr ::= HlslExpr, ''?'', HlslExpr, '':'', HlslExpr", SDT_PassCode, opPriority1);
            AddRule("HlslAssignOp ::= ''='' | ''+='' | ''-='' | ''*='' | ''/=''", SDT_PassCode);
            AddRule("HlslOp ::= ''+'' | ''-'' | ''*'' | ''/'' | ''>'' | ''<'' | ''=='' | ''>='' | ''<='' | ''!='' | ''&&'' | ''||''", SDT_PassCode);
            AddRule("HlslUnaryOp ::= ''!'' | ''-''", SDT_PassCode);
            AddRule("HlslPrePostOp ::= ''++'' | ''--''", SDT_PassCode);
            // FuncCallArg
            /// A single parameter in a function call, differently from hlsl 
            /// here the modifier should be repeated
            AddRule("FuncCallArg ::= [InputModifier], HlslExpr", STD_FuncCallArg);
            AddRule("FuncCall ::= Name, ''('', '')''", SDT_FuncCall);
            AddRule("FuncCall ::= Name, ''('', FuncCallArg, {'','', FuncCallArg }*, '')''", SDT_FuncCall);
            AddRule("FuncCall ::= VectorType, ''('', HlslExpr, {'','', HlslExpr}*, '')''", SDT_FuncCall); // vector contructors

            // Generic productions
            AddRule("Name ::= #name | #id", SDT_InstanceValue);
        }

        #region STDs

        private object STD_Program(ISDTArgs<object> args)
        {
            ShaderSrcFile shaderFile = new ShaderSrcFile();

            // shader name
            shaderFile.Name = args.Values["ShaderDecl"].ToString();

            // usings 
            foreach (RemovedCode includedShaderName in args.Values.GetList("UsingDirective"))
                shaderFile.Includes.Add((string)includedShaderName);

            // effetcs
            foreach (ParsedCode<ShaderSrcFile.EffectInfo> e in args.Values.GetList("EffectDeclaration"))
                shaderFile.Effects.Add(e.Value);

            // constants
            foreach (ParsedCode<ShaderSrcFile.ConstantInfo> c in args.Values.GetList("UniformDeclaration"))
                if (!c.Value.IsConstant) shaderFile.Constants.Add(c.Value);

            // textures
            foreach (ParsedCode<ShaderSrcFile.TextureInfo> tex in args.Values.GetList("TexDeclaration"))
                shaderFile.Textures.Add(tex.Value);

            // layouts
            foreach (ParsedCode<ShaderSrcFile.LayoutInfo> layout in args.Values.GetList("LayoutDeclaration"))
                shaderFile.Layouts.Add(layout.Value);

            // template types
            foreach (ParsedCode<ShaderSrcFile.TemplateTypeInfo> t in args.Values.GetList("TemplateFuncDeclaration"))
                if (!string.IsNullOrEmpty(t.Value.FuncName))
                    shaderFile.MergeTemplateType(t.Value);


            // functions
            foreach (ParsedCode<FuncDeclaration> func in args.Values.GetList("FuncDeclaration"))
                switch (func.Value.ProgramType)
                {
                    case TOKEN_VSPROGRAM:
                        shaderFile.VSList.Add(new ShaderSrcFile.ProgramInfo()
                        {
                            Type = ShaderSrcFile.ProgramType.VertexShader,
                            EntryPoint = func.Value.Name,
                            FromLayout = func.Value.ArgTypes[0],
                            ToLayout = func.Value.Type
                        });
                        break;

                    case TOKEN_PSPROGRAM:
                        shaderFile.PSList.Add(new ShaderSrcFile.ProgramInfo()
                        {
                            Type = ShaderSrcFile.ProgramType.PixelShader,
                            EntryPoint = func.Value.Name,
                            FromLayout = func.Value.ArgTypes[0],
                            ToLayout = func.Value.Type
                        });
                        break;

                    case "Generic":
                        if (!string.IsNullOrEmpty(func.Value.TemplTypeName))
                        {
                            shaderFile.MergeTemplateType(new ShaderSrcFile.TemplateTypeInfo() { FuncName = func.Value.Name, TypeImplName = func.Value.TemplTypeName });
                        }
                        break;
                }

            

            // variants
            foreach (ParsedCode<ShaderSrcFile.Variant> v in args.Values.GetList("VariantDeclaration"))
                shaderFile.Variants.Add(v.Value);

            // structs
            foreach (ParsedCode<ShaderSrcFile.StructInfo> s in args.Values.GetList("StructDeclaration"))
                shaderFile.Structs.Add(s.Value);

            // external textures
            shaderFile.ExternalTextures = externalTextures;

            // code formatting
            shaderFile.Body = CleanHlslCode(args.ParsedCode);

            return shaderFile;
        }

        private object SDT_VariantDeclaration(ISDTArgs<object> args)
        {
            // retrieve user values for the variant
            List<string> values = new List<string>();
            for (int i = 1; i < args.Values.GetInstanceCount("Name"); i++)
                values.Add(args.Values["Name", i].ToString());

            if (values.Count < 2)
            {
                values.Clear();
                values.Add("False");
                values.Add("True");
            }

            // create and return the variant
            ShaderSrcFile.Variant variant = new ShaderSrcFile.Variant(args.Values["Name"].ToString(), values.ToArray());
            curVariants.Add(variant);
            return new ParsedCode<ShaderSrcFile.Variant>(string.Empty, variant);
        }

        public struct FuncDeclaration
        { 
            public string ProgramType, Name, Type, Body, ArgListCode, TemplTypeName;
            public string[] ArgTypes, ArgNames, ArgModifiers;

            public void ParseArgList()
            {
                if (string.IsNullOrEmpty(ArgListCode))
                {
                    ArgTypes = new string[0];
                    ArgNames = new string[0];
                    ArgModifiers = new string[0];
                    return;
                }

                string[] argList = ArgListCode.Split(',');
                ArgTypes = new string[argList.Length];
                ArgNames = new string[argList.Length];
                ArgModifiers = new string[argList.Length];
                for (int i = 0; i < argList.Length; i++)
                {
                    string[] argElems = argList[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    ArgTypes[i] = argElems[argElems.Length - 2];
                    ArgNames[i] = argElems[argElems.Length - 1];
                    if (argElems.Length > 2)
                        ArgModifiers[i] = argElems[argElems.Length - 3];
                }
            }
        }

        private object SDT_FuncDeclaration(ISDTArgs<object> args)
        {
            TypeNameBlock typeNameBlock = (args.Values["TypeNameBlock"] as ParsedCode<TypeNameBlock>).Value;

            FuncDeclaration funcDecl = new FuncDeclaration();
            funcDecl.Name = typeNameBlock.Name;
            funcDecl.Type = typeNameBlock.Type;
            funcDecl.ProgramType = "Generic";
            funcDecl.Body = (args.Values["CodeBody"] as ParsedCode<string>).Value;

            if (args.Tokens.Contains("<"))
            {
                funcDecl.TemplTypeName = args.Values["Name"].ToString();
            }

            if (args.Values.Contains("ArgList"))
            {
                funcDecl.ArgListCode = (args.Values["ArgList"] as ParsedCode<string>).Value.Trim();
                // exit save texture params scope of this function.
                texParamStack.Pop();
            }

            funcDecl.ParseArgList();

            if (args.Values.Contains("ShaderProgramType"))
            {
                if (funcDecl.ArgTypes == null || funcDecl.ArgTypes.Length != 1)
                    throw new SemanticError(args.CodeLine, "SC0009", args.ParsedCode, $"Missing or invalid input interpolants for the shader program {funcDecl.Name}.");
                funcDecl.ProgramType = args.Values["ShaderProgramType"].ToString();
                funcDecl.Name = funcDecl.ProgramType + "_" + funcDecl.Name;

            }

            // save parsed function
            funcDeclarations[funcDecl.Name] = funcDecl;

            // prepare and return function code block

            // create a copy of the decl for the compiler, with the platform-specific args
            FuncDeclaration compilerDecl = new FuncDeclaration() {
                ProgramType = funcDecl.ProgramType,
                Name = funcDecl.Name,
                Type = funcDecl.Type,
                Body = funcDecl.Body,
                TemplTypeName = funcDecl.TemplTypeName
            };
            if (args.Values.Contains("ArgList"))
                compilerDecl.ArgListCode = (args.Values["ArgList"] as ParsedCode<string>).Code.Trim();
            compilerDecl.ParseArgList();
            string funcDeclCode = NativeCompiler.ProduceFunctionCode(compilerDecl);
            return new ParsedCode<FuncDeclaration>(funcDeclCode + Environment.NewLine, compilerDecl);
        }

        private object SDT_TemplateFuncDeclaration(ISDTArgs<object> args)
        {
            ParsedCode<TypeNameBlock> typeName = args.Values["TypeNameBlock"] as ParsedCode<TypeNameBlock>;
            string argStr = "";
            if(args.Values.Contains("ArgList"))
                argStr = (args.Values["ArgList"] as ParsedCode<string>).Code;
            string forwardDeclStr = $"{typeName.Code}({argStr});\n";

            ShaderSrcFile.TemplateTypeInfo templateType = new ShaderSrcFile.TemplateTypeInfo();

            if (args.Tokens.Contains("<"))
            {
                templateType.FuncName = typeName.Value.Name;
                templateType.TypeDeclName = args.Values["Name"].ToString();
            }

            // create a function from template: currently used to check modifiers if only the template is curretly declared
            FuncDeclaration templateFunc = new FuncDeclaration();
            templateFunc.Name = typeName.Value.Name;
            if (args.Values.Contains("ArgList"))
                templateFunc.ArgListCode = args.Values["ArgList"].ToString().Trim();
            templateFunc.ParseArgList();
            funcDeclarations[templateFunc.Name] = templateFunc;

            return new ParsedCode<ShaderSrcFile.TemplateTypeInfo>(forwardDeclStr, templateType);
        }

        private object SDT_TexDeclaration(ISDTArgs<object> args)
        {
            ShaderSrcFile.TextureInfo tex = new ShaderSrcFile.TextureInfo();
            tex.Type = "Color";
            tex.Name = args.Values["Name"].ToString();
            tex.IsGlobal = args.Values.Contains("VarModifier") && args.Values["VarModifier"].ToString().Trim() == TOKEN_MOD_GLOBAL;

            foreach (string samplerOpt in args.Values.GetList("SamplerOption"))
            {
                string cleanSamplerOpt = samplerOpt.Replace(" ", "");

                // parse sampler option
                tex.BindingOptions |= (TextureBindingOptions)Enum.Parse(typeof(TextureBindingOptions), cleanSamplerOpt);
            }

            texDeclarations[tex.Name] = tex;
            return new ParsedCode<ShaderSrcFile.TextureInfo>(string.Empty, tex);
        }

        private object SDT_UniformDeclaration(ISDTArgs<object> args)
        {
            ShaderSrcFile.ConstantInfo c = new ShaderSrcFile.ConstantInfo();
            c.ArraySize = 1;
            if (args.Values.Contains("VarIndexing"))
            {
                int.TryParse((args.Values["VarIndexing"] as ParsedCode<string>).Value, out c.ArraySize); // TODO parse defines and assign its value here if a name is found
                c.IsArray = true;
            }

            if (args.Values.Contains("VarModifier"))
            {
                string modifier = args.Values["VarModifier"].ToString().Trim();
                c.IsGlobal = modifier.Contains(TOKEN_MOD_GLOBAL);
                c.IsDynamic = modifier.Contains(TOKEN_MOD_DYNAMIC);
                c.IsConstant = modifier == TOKEN_MOD_CONST;
            }

            TypeNameBlock typeNameBlock = (args.Values["TypeNameBlock"] as ParsedCode<TypeNameBlock>).Value;
            c.Name = typeNameBlock.Name;
            c.Type = typeNameBlock.Type;
            return new ParsedCode<ShaderSrcFile.ConstantInfo>(c.IsConstant ? args.ParsedCode.Replace(TOKEN_MOD_CONST, NativeCompiler.ProduceConstModifier()) + Environment.NewLine : string.Empty, c);
        }

        private object SDT_StructDeclaration(ISDTArgs<object> args)
        {
            return new ParsedCode<ShaderSrcFile.StructInfo>("", new ShaderSrcFile.StructInfo() { Name = args.Values["Name"].ToString(), SrcCode = args.ParsedCode });
        }

        private object SDT_LayoutDeclaration(ISDTArgs<object> args)
        {
            ShaderSrcFile.LayoutInfo layout = new ShaderSrcFile.LayoutInfo();
            layout.Name = args.Values["Name"].ToString();
            layout.Elements = new List<ShaderSrcFile.LayoutElemInfo>();

            for (int i = 0; i < args.Values.GetInstanceCount("SemanticName"); i++)
            {
                layout.Elements.Add(new ShaderSrcFile.LayoutElemInfo()
                {
                    SemanticName = args.Values["SemanticName", i].ToString(),
                    Type = ((ParsedCode<TypeNameBlock>)args.Values["TypeNameBlock", i]).Value.Type,
                    Name = ((ParsedCode<TypeNameBlock>)args.Values["TypeNameBlock", i]).Value.Name
                });
            }
            
            return new ParsedCode<ShaderSrcFile.LayoutInfo>(NativeCompiler.ProduceLayoutDecl(layout), layout);
        }

        private object SDT_ArgList(ISDTArgs<object> args)
        {
            // open a new texture param scope
            texParamStack.Push(new HashSet<string>());

            string argListCode = args.ParsedCode;

            // replace the "texture" type as a function parameter 
            argListCode = Regex.Replace(argListCode, @"\btexture\s(\w+)", match =>
            {
                string texParamName = match.Groups[1].Value;
                texParamStack.Peek().Add(texParamName);
                return NativeCompiler.ProduceTextureParamDecl(texParamName);
            });

            // return both the compiler specific code and the original code (used for the original args validation)
            return new ParsedCode<string>(argListCode, args.ParsedCode);
        }

        private struct TypeNameBlock { public string Type, Name; }
        private object SDT_TypeNameBlock(ISDTArgs<object> args)
        {
            TypeNameBlock typeNameBlock;
            if (args.Values.Contains("VarType"))
            {
                typeNameBlock.Type = args.Values["VarType"].ToString();
                typeNameBlock.Name = args.Values["Name"].ToString();
            }
            else
            {
                typeNameBlock.Type = args.Values["Name", 0].ToString();
                typeNameBlock.Name = args.Values["Name", 1].ToString();
            }

            return new ParsedCode<TypeNameBlock>(typeNameBlock.Type + " " + typeNameBlock.Name, typeNameBlock);
        }

        private object SDT_VarIndexing(ISDTArgs<object> args)
        {
            return new ParsedCode<string>(args.ParsedCode, args.Values["HlslExpr"].ToString());
        }

        private object SDT_EffectDeclaration(ISDTArgs<object> args)
        {
            List<ShaderSrcFile.TemplateInfo> emptyTemplate = new List<ShaderSrcFile.TemplateInfo>();
            emptyTemplate.Add(new ShaderSrcFile.TemplateInfo() { LayoutType = "", Name = "" });

            return new ParsedCode<ShaderSrcFile.EffectInfo>(string.Empty, new ShaderSrcFile.EffectInfo()
            {
                Name = args.Values["Name", 0].ToString(),
                IsTemplate = args.Tokens.Contains(TOKEN_TEMPLATE_EFFECT),
                Templates = emptyTemplate,
                VsName = TOKEN_VSPROGRAM + "_" + args.Values["Name", 1].ToString(),
                PsName = TOKEN_PSPROGRAM + "_" + args.Values["Name", 2].ToString(),
                SupportsInstancing = args.Tokens.Contains(TOKEN_INSTANCING_SUPPORT)
            });
        }

        private object SDT_DerivedEffectDeclaration(ISDTArgs<object> args)
        {
            // parse templates
            List<ShaderSrcFile.TemplateInfo> templates = new List<ShaderSrcFile.TemplateInfo>();
            for (int i = 1; i < args.Values.GetInstanceCount("Name"); i += 2)
            {
                templates.Add(new ShaderSrcFile.TemplateInfo()
                {
                    Name = args.Values["Name", i].ToString(),
                    LayoutType = args.Values["Name", i + 1].ToString(),
                });
            }

            return new ParsedCode<ShaderSrcFile.EffectInfo>(string.Empty, new ShaderSrcFile.EffectInfo()
            {
                Name = args.Values["Name", 0].ToString(),
                IsTemplate = args.Tokens.Contains(TOKEN_TEMPLATE_EFFECT),
                Templates = templates,
                VsName = string.Empty /* inherited */,
                PsName = string.Empty /* inherited */,
                SupportsInstancing = false /* inherited */
            });
        }

        private object SDT_DerivedTemplateDeclaration(ISDTArgs<object> args)
        {
            List<ShaderSrcFile.TemplateInfo> baseTemplate = new List<ShaderSrcFile.TemplateInfo>();
            baseTemplate.Add(new ShaderSrcFile.TemplateInfo() { LayoutType = "", Name = args.Values["Name", 1].ToString() });

            return new ParsedCode<ShaderSrcFile.EffectInfo>(string.Empty, new ShaderSrcFile.EffectInfo()
            {
                Name = args.Values["Name", 0].ToString(),
                IsTemplate = args.Tokens.Contains(TOKEN_TEMPLATE_EFFECT),
                Templates = baseTemplate,
                VsName = string.Empty /* inherited */,
                PsName = string.Empty /* inherited */,
                SupportsInstancing = false /* inherited */
            });
        }


        private object SDT_CodeBody(ISDTArgs<object> args)
        {
            // code formatting
            string formattedBody = args.ParsedCode.Replace("{", Environment.NewLine + "{" + Environment.NewLine) + Environment.NewLine;
            string[] bodyLines = formattedBody.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 0; i < bodyLines.Length; i++)
                bodyLines[i] = ((i < 2 || i > bodyLines.Length - 3) ? "" : "\t") + bodyLines[i].Trim(' ');

            string fullBody = string.Join(Environment.NewLine, bodyLines);
            int start = fullBody.IndexOf("{") + 1, end = fullBody.LastIndexOf("}");
            return new ParsedCode<string>(fullBody, fullBody.Substring(start, end - start).Trim());
        }

        private object SDT_CodeBodyOptBracket(ISDTArgs<object> args)
        {
            if (args.Values.Contains("CodeBody"))
                return args.Values["CodeBody"];
            else
                return new ParsedCode<string>(args.Values["CodeBodyContent"].ToString(), args.Values["CodeBodyContent"].ToString());
        }


        private object SDT_IfBlock(ISDTArgs<object> args)
        {
            string conditionCode = args.Values["HlslExpr"].ToString().Trim();

            // all other if blocks: just return the parsed code
            return args.ParsedCode;
        }

        private struct ElifBlock { public string Body, Condition; }
        private object SDT_ElifBlock(ISDTArgs<object> args)
        {
            ElifBlock elif = new ElifBlock();
            elif.Condition = args.Values["HlslExpr"].ToString().Trim();
            elif.Body = ((ParsedCode<string>)args.Values["CodeBodyOptBracket"]).Value;
            return new ParsedCode<ElifBlock>(args.ParsedCode, elif);
        }

        private object SDT_ElseBlock(ISDTArgs<object> args)
        {
            return new ParsedCode<string>(args.ParsedCode, ((ParsedCode<string>)args.Values["CodeBodyOptBracket"]).Value);
        }

        private object SDT_UsingDirective(ISDTArgs<object> args)
        {
            return (RemovedCode)args.Values["Name"].ToString();
        }

        private object SDT_ShaderDecl(ISDTArgs<object> args)
        {
            shaderName = args.Values["Name"].ToString();
            StringBuilder headerCode = new StringBuilder();
            headerCode.AppendFormatLine("#define {0}", destPlatform.Description);
            return new ParsedCode<string>(headerCode.ToString(), args.Values["Name"].ToString());
        }

        struct FuncCallArg
        {
            public string Expression, Modifier;
        }

        private object STD_FuncCallArg(ISDTArgs<object> args)
        {
            FuncCallArg farg;
            farg.Expression = args.Values["HlslExpr"].ToString().Trim();
            farg.Modifier = string.Empty;
            if (args.Values.Contains("InputModifier"))
                farg.Modifier = args.Values["InputModifier"].ToString();
            return new ParsedCode<FuncCallArg>(farg.Expression, farg);
        }

        private object SDT_FuncCall(ISDTArgs<object> args)
        {
            string parsedFunctionCall = args.ParsedCode;

            if (args.Values.Contains("Name"))
            {
                // function calls, look for special dfx calls to be managed separately from each API
                string funcName = args.Values["Name"].ToString();

                // prepare args
                int argsCount = args.Values.GetInstanceCount("FuncCallArg");
                string[] funcArgs = new string[argsCount];
                for (int i = 0; i < argsCount; i++)
                    funcArgs[i] = (args.Values["FuncCallArg", i] as ParsedCode<FuncCallArg>).GetSrcCode();

                if (funcName == INSTRINSIC_SAMPLING_FUNC && argsCount == 2)
                {
                    parsedFunctionCall = NativeCompiler.ProduceSamplingInstruction(GetTextureDecl(args, funcArgs[0]), funcArgs[1]);
                }
                else if (funcName == INSTRINSIC_SAMPLING_LOD_FUNC && argsCount == 3)
                {
                    parsedFunctionCall = NativeCompiler.ProduceSamplingInstruction(GetTextureDecl(args, funcArgs[0]), funcArgs[1], funcArgs[2]);
                }
                else if (funcName == INSTRINSIC_SAMPLING_LOD0_FUNC && argsCount == 2)
                {
                    parsedFunctionCall = NativeCompiler.ProduceSamplingInstruction(GetTextureDecl(args, funcArgs[0]), funcArgs[1], "0");
                }
                else if (funcName == INSTRINSIC_TEXEL_SIZE_FUNC)
                {
                    parsedFunctionCall = NativeCompiler.ProduceTexelSizeInstruction(GetTextureDecl(args, funcArgs[0]));
                }
                else
                {
                    // default function call management:
                    FuncDeclaration fDecl;
                    bool declFound = funcDeclarations.TryGetValue(funcName, out fDecl);

                    // process parameters
                    for (int i = 0; i < argsCount; i++)
                    {
                        // validate parameter modifiers
                        string modifier = (args.Values["FuncCallArg", i] as ParsedCode<FuncCallArg>).Value.Modifier;
                        string expectedModifier = modifier; // TODO: won't rise error if not found, this is the case of included external functions that cannot be detected for now
                        if (declFound && fDecl.ArgModifiers.Length > i && fDecl.ArgModifiers[i] != null)
                        {
                            expectedModifier = fDecl.ArgModifiers[i];
                        }

                        if (modifier != expectedModifier)
                        {
                            expectedModifier = expectedModifier.Length > 0 ? expectedModifier : "no modifier";
                            throw new SemanticError(-1, "SC0008", args.ParsedCode, $"The argument {funcArgs[i]} of a call to function {funcName} have an invalid modifier! ({expectedModifier} expected)");
                        }
                        
                        // replace texture input parameters with API specific code
                        if (texDeclarations.ContainsKey(funcArgs[i]) || texParamStack.Count > 0 && texParamStack.Peek().Contains(funcArgs[i]))
                            funcArgs[i] = NativeCompiler.ProduceTextureParamUsage(GetTextureDecl(args, funcArgs[i]));
                    }

                    // build function call code
                    parsedFunctionCall = funcName + "(" + string.Join(",", funcArgs) + ")";
                }
            }
            else
            {
                // vector literal constructor
            }

            return parsedFunctionCall;
        }

        private ShaderSrcFile.TextureInfo GetTextureDecl(ISDTArgs<object> args, string textureName)
        {
            // from call params
            if(texParamStack.Count > 0 && texParamStack.Peek().Contains(textureName))
                return new ShaderSrcFile.TextureInfo() { Name = textureName, Type = "textureParam", IsParameter = true };
            // from declaration
            if (texDeclarations.ContainsKey(textureName))
                return texDeclarations[textureName];

            // assume its an external texture, will be checked after include resolution
            externalTextures.Add(textureName);
            return new ShaderSrcFile.TextureInfo() { Name = textureName };
        }

        private object SDT_Literal(ISDTArgs<object> args)
        {
            string literalStr = args.ParsedCode;
            if (args.Tokens.Contains("f"))
                literalStr = Regex.Replace(literalStr, "[ ]+f[ ]*", "f"); // remove spaces around "f" float literal
            return literalStr;
        }

        private object SDT_PassCode(ISDTArgs<object> args)
        {
            return args.ParsedCode;
        }

        private object SDT_PassCodeLine(ISDTArgs<object> args)
        {
            return args.ParsedCode + Environment.NewLine;
        }

        private object SDT_InstanceValue(ISDTArgs<object> args)
        {
            if (args.Values.Count == 1)
                return args.Values.GetInstanceValue().ToString();
            else
                return args.Tokens.GetInstanceValue().StringValue;
        }

        #endregion // SDTs

        public override string DebugName => "DFXShaderCompiler";

        /// <summary>
        /// An optional filter function that returns true when a shader should be included in the compilation process.
        /// </summary>
        public Func<string, bool> ShaderNameFilter { get; set; }

        /// <summary>
        /// If set before compile, the bindings are added to this table (instea of creating a new one)
        /// </summary>
        public ShaderBindingTable InputBindingTable { get; set; }

        /// <summary>
        /// Platform-specific compiler implementation.
        /// </summary>
        public ShaderCompiler NativeCompiler { get; private set; }

        protected override void ProcessIntermediateOutput(ref object ilOutput)
        {

        }

        protected override void OnBeforeCompile()
        {
            base.OnBeforeCompile();

            // reset parsing context cache
            texDeclarations = new Dictionary<string, ShaderSrcFile.TextureInfo>();
            funcDeclarations = new Dictionary<string, FuncDeclaration>();
            curVariants = new List<ShaderSrcFile.Variant>();
            externalTextures = new HashSet<string>();
        }

        protected override CodeProject BuildFinalOutput(List<object> ilOutputs)
        {
            // prepare result project
            CodeProject result = CodeProject.Empty;
            result.ProjectName = "Shaders";

            // initialize compiler and binding table
            List<ShaderSrcFile> shaders = new List<ShaderSrcFile>();
            foreach (ShaderSrcFile shaderFile in ilOutputs) 
                shaders.Add(shaderFile);
            ShaderBindingTable bindings = new ShaderBindingTable();
            NativeCompiler.Initialize();

            // compute a list of global constants that should be forward-signaled to the compiler
            {
                List<ShaderSrcFile.ConstantInfo> globalConsts = new List<ShaderSrcFile.ConstantInfo>();
                List<ShaderSrcFile.TextureInfo> globalTextures = new List<ShaderSrcFile.TextureInfo>();
                foreach (ShaderSrcFile s in shaders)
                {
                    foreach (ShaderSrcFile.ConstantInfo c in s.Constants)
                        if (c.IsGlobal) globalConsts.Add(c);
                    foreach (ShaderSrcFile.TextureInfo t in s.Textures)
                        if (t.IsGlobal) globalTextures.Add(t);
                }
                NativeCompiler.SetGlobals(globalConsts, globalTextures, bindings);
            }

            try
            {
                List<ShaderSrcFile> resolvedShaders = new List<ShaderSrcFile>();
                
                //resolve shaders
                {
                    // resolve includes
                    List<ShaderSrcFile> usingMergeShaders = new List<ShaderSrcFile>();
                    foreach (ShaderSrcFile s in shaders)
                    {
                        ShaderSrcFile usingMerge = s.Clone();
                        usingMergeShaders.Add(usingMerge);
                        usingMerge.MergeAllIncludes(shaders);

                        if(usingMerge.ExternalTextures.Count > 0)
                        {
                            throw new SemanticError(-1, "DFX0002", shaderName, "Texture declarations not found: " + string.Join(",", usingMerge.ExternalTextures));
                        }

                    }

                    // bind all variants
                    foreach (ShaderSrcFile s in usingMergeShaders)
                        bindings.BindVariants(s);

                    // unroll all variants
                    foreach (ShaderSrcFile s in usingMergeShaders)
                        resolvedShaders.AddRange(s.UnrollAllVariants());

                    // resolve variants code
                    foreach (ShaderSrcFile s in resolvedShaders)
                        s.ApplyCurrentVariant();
                }

                // bind and generate shader code for the destination platform
                List<StringBuilder> resolvedCode = new List<StringBuilder>();
                foreach (ShaderSrcFile s in resolvedShaders)
                {
                    // Prepare a solved src container for the current shader
                    StringBuilder shaderCode = new StringBuilder();
                    resolvedCode.Add(shaderCode);

                    // Bind shader inputs (constants / textures)
                    NativeCompiler.BindInputs(s, bindings);

                    // Generate inputs code
                    shaderCode.AppendLine(NativeCompiler.ProduceInputDeclarations(s, bindings));

                    // Generate template types
                    foreach(ShaderSrcFile.TemplateTypeInfo t in s.TemplateTypes)
                    {
                        if (!string.IsNullOrEmpty(t.TypeDeclName))
                        {
                            if (!string.IsNullOrEmpty(t.TypeImplName))
                                shaderCode.AppendLine($"#define {t.TypeDeclName} {t.TypeImplName}");
                            else
                            {
                                // check if the shader has any compilable effect
                                bool hasEffectImplementation = false;
                                foreach (ShaderSrcFile.EffectInfo e in s.Effects)
                                    hasEffectImplementation = hasEffectImplementation | !e.IsTemplate;

                                // if it needs to be compiled, all the template function must be implemented, an incomplete template is invalid
                                if(hasEffectImplementation)
                                    throw new SemanticError(-1, "SC0005", s.Name, $"The template function '{t.FuncName}' in shader '{s.Name}' is not implemented, or the implementation does not specify the template type '{t.TypeDeclName}'.");
                            }
                        }
                    }

                    // Output struct declarations
                    foreach (ShaderSrcFile.StructInfo strDecl in s.Structs)
                    {
                        shaderCode.AppendLine(strDecl.SrcCode);
                    }

                    // Add code body
                    NativeCompiler.PreprocessShaderCode(ref s.Body);
                    shaderCode.AppendLine(s.Body);

                    // Bind effects
                    BindEffects(s, bindings);

#if DEBUG
                    // Add shader source code to the output
                    result.AddSource(new CodeFile(shaderCode.ToString(), GetShaderFileName(destPlatform, s.Name, s.CurrentVariantValues), CodeFileType.ExternalFile));
#endif
                }

                // compile all translated shaders
                for (int i = 0; i < resolvedShaders.Count; i++)
                {
                    ShaderSrcFile s = resolvedShaders[i];

                    // check if the current shader should actually be recompiled (or the old bytecode should be recycled)
                    bool shouldRecompile = ShaderNameFilter == null || ShaderNameFilter(s.Name);

                    // used for lazy-log to avoid logging library or templates
                    bool shaderLogged = false;

                    string shaderCode = resolvedCode[i].ToString();

                    for(int ei = 0; ei < s.Effects.Count; ei++)
                    {
                        // skip templates
                        if (s.Effects[ei].IsTemplate)
                            continue;

                        // log current compile process to console
                        if (!shaderLogged)
                        {
                            if (i == 0 || s.Name != resolvedShaders[i - 1].Name)
                                LogShader(s);
                            if (s.VariantCount > 1)
                                LogColor("Variant: " + s.CurrentVariantValues.ToKeyValueString(), ConsoleColor.DarkCyan);

                            shaderLogged = true;
                        }

                        // foreach template
                        foreach (KeyValuePair<ShaderSrcFile.TemplateInfo, ShaderSrcFile.EffectInfo> eSolved in SolveEffectTemplates(s, s.Effects[ei]))
                        {
                            ShaderSrcFile.EffectInfo e = eSolved.Value;
                            LogColor($"Effect: {e.Name}" + (e.Templates.Count == 1 ? "" : $", Template: {eSolved.Key.Name}"), ConsoleColor.DarkMagenta);

                            // calc template-specific defines
                            string templateDefines = "";
                            if (!string.IsNullOrEmpty(eSolved.Key.Name))
                            {
                                templateDefines += $"#define {TOKEN_AUTO_VERTEXTYPE} {eSolved.Key.LayoutType}\n";
                            }

                            // calc source code for the current template
                            string solvedShaderCode = templateDefines + shaderCode;

                            // compile VS
                            {
                                ShaderSrcFile.ProgramInfo vs = s.VSList.Find(vsi => vsi.EntryPoint == e.VsName);

                                // propagate instanced flag to vs programs
                                vs.SupportInstancing = e.SupportsInstancing;

                                

                                string vsProgramName = GetProgramName(destPlatform, s.Name, vs.EntryPoint, eSolved.Key.Name, s.CurrentVariantValues);

                                if (!bindings.ContainsProgram(vsProgramName))
                                {
                                    byte[] vsBlob = CompileNativeProgram(vs, solvedShaderCode, vsProgramName, shouldRecompile);
                                    bindings.BindProgram(vsProgramName, vsBlob);
#if DEBUG
                                    // Add disassembly output for this program
                                    result.AddSource(new CodeFile(NativeCompiler.GenerateDebugInfo(vsBlob), GetShaderFileName(destPlatform, s.Name, s.CurrentVariantValues) + ".debuginfo.txt", CodeFileType.ExternalFile));
#endif
                                }
                            }

                            // compile PS
                            {
                                ShaderSrcFile.ProgramInfo ps = s.PSList.Find(psi => psi.EntryPoint == e.PsName);

                                string psProgramName = GetProgramName(destPlatform, s.Name, ps.EntryPoint, eSolved.Key.Name, s.CurrentVariantValues);
                                if (!bindings.ContainsProgram(psProgramName))
                                {
                                    byte[] psBlob = CompileNativeProgram(ps, solvedShaderCode, psProgramName, shouldRecompile);
                                    bindings.BindProgram(psProgramName, psBlob);

#if DEBUG
                                    // Add disassembly output for this program
                                    result.AddSource(new CodeFile(NativeCompiler.GenerateDebugInfo(psBlob), GetShaderFileName(destPlatform, s.Name, s.CurrentVariantValues) + ".debuginfo.txt", CodeFileType.ExternalFile));
#endif
                                }
                            }
                        }

                    } // for each effect

                } // for each resolved shader

                //add the binding table as result file
                result.AddBinary(new BinFile(bindings.ToByteArray(), ShaderCompiler.GetBindingTableFilename(destPlatform), CodeFileType.RuntimeResource));
            }
            finally
            {
                Reset(); //reset after processing (this is called only one time)
            }

            return result;
        }

        private IEnumerable<KeyValuePair<ShaderSrcFile.TemplateInfo, ShaderSrcFile.EffectInfo>> SolveEffectTemplates(ShaderSrcFile shader, ShaderSrcFile.EffectInfo e)
        {
            // foreach template
            for (int ti = 0; ti < e.Templates.Count; ti++)
            {
                // update effect inheriting from template
                if (!string.IsNullOrEmpty(e.Templates[ti].Name))
                {
                    // find the template effect
                    int eTemplID = shader.Effects.FindIndex(eTempl => eTempl.Name == e.Templates[ti].Name);
                    if (eTemplID < 0)
                        throw new SemanticError(-1, "SC0006", "", $"A definition for the template {e.Templates[ti].Name} implemented by effect {e.Name} was not found.");
                    ShaderSrcFile.EffectInfo eTemplate = shader.Effects[eTemplID];

                    // find the first anchestor effect that is actually implemented
                    while (!string.IsNullOrEmpty(eTemplate.Templates[0].Name))
                        eTemplate = shader.Effects.Find(eTempl => eTempl.Name == eTemplate.Templates[0].Name);

                    // replace info with template info
                    e.IsTemplate = false;
                    e.PsName = eTemplate.PsName;
                    e.SupportsInstancing = eTemplate.SupportsInstancing;
                    e.VsName = eTemplate.VsName;
                }

                yield return new KeyValuePair<ShaderSrcFile.TemplateInfo, ShaderSrcFile.EffectInfo>(e.Templates[ti], e);
            }
        }

        private byte[] CompileNativeProgram(ShaderSrcFile.ProgramInfo program, string sourceCode, string outputName, bool forceRecompile)
        {
            byte[] programBlob = null;

            // try recovering an older version from the provided table if available and recompile is not needed
            if (!forceRecompile)
            {
                if (InputBindingTable.TryGetProgram(outputName, out programBlob))
                {
                    Console.WriteLine($"Skipped: {program.EntryPoint}");
                }
            }

            // compile if no bytecode is available
            if (programBlob == null)
                programBlob = NativeCompiler.CompileShader(sourceCode, program);

            return programBlob;
        }

        private void LogShader(ShaderSrcFile s)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Exporting shader {0} for {1} ", s.Name, destPlatform);
            if(s.Variants.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("({0} variants)", s.VariantCount);
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }

        private void LogColor(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public override void Reset()
        {
            base.Reset();
            texParamStack = new Stack<HashSet<string>>();
        }

        private void BindEffects(ShaderSrcFile shader, IEffectBinder binder)
        {
            for (int i = 0; i < shader.Effects.Count; i++)
            {
                ShaderSrcFile.EffectInfo e = shader.Effects[i];

                // skip templates
                if (e.IsTemplate)
                    continue;

                // foreach template
                foreach(KeyValuePair<ShaderSrcFile.TemplateInfo, ShaderSrcFile.EffectInfo> eSolved in SolveEffectTemplates(shader, e))
                {
                    BindEffect(shader, binder, eSolved.Value, eSolved.Key);
                }
            }
        }

        private void BindEffect(ShaderSrcFile shader, IEffectBinder binder, ShaderSrcFile.EffectInfo e, ShaderSrcFile.TemplateInfo template)
        {
            // search vs
            int vsIndex = shader.VSList.FindIndex(vs => vs.EntryPoint == e.VsName);
            if (vsIndex < 0)
                throw new SemanticError(-1, "SC0001", "", string.Format("A definition for vertex shader \"{0}\" referenced in effect \"{1}\" cannot be found.", e.VsName, e.Name));
            // search ps
            int psIndex = shader.PSList.FindIndex(ps => ps.EntryPoint == e.PsName);
            if (psIndex < 0)
                throw new SemanticError(-1, "SC0002", "", string.Format("A definition for pixel shader \"{0}\" referenced in effect \"{1}\" cannot be found.", e.PsName, e.Name));
            // search input layout
            int inLayoutIndex = -1;
            {
                // retrieve layout name
                string layoutName = shader.VSList[vsIndex].FromLayout;
                if (!string.IsNullOrEmpty(template.Name))
                {
                    layoutName = template.LayoutType; // use the layout specified with the template when inheriting from one
                }

                // search for its index
                if (!string.IsNullOrEmpty(layoutName))
                {
                    inLayoutIndex = shader.Layouts.FindIndex(l => l.Name == layoutName);
                    if (inLayoutIndex < 0)
                        throw new SemanticError(-1, "SC0003", "", string.Format("A definition for the input layout \"{0}\" referenced in VS \"{1}\" cannot be found.", shader.VSList[vsIndex].FromLayout, e.VsName));
                }
            }

            // search output layout
            int outLayoutIndex = shader.Layouts.FindIndex(l => l.Name == shader.PSList[psIndex].ToLayout);
            if (outLayoutIndex < 0)
                throw new SemanticError(-1, "SC0004", "", string.Format("A definition for the output layout \"{0}\" referenced in PS \"{1}\" cannot be found.", shader.PSList[psIndex].ToLayout, e.PsName));

            // bind effect
            try
            {
                binder.BindEffect(new EffectBinding()
                {
                    EffectName = e.Name,
                    Template = template.Name,
                    ShaderName = shader.Name,
                    VariantValues = new Dictionary<string, string>(shader.CurrentVariantValues),
                    VSName = GetProgramName(destPlatform, shader.Name, e.VsName, template.Name, shader.CurrentVariantValues),
                    PSName = GetProgramName(destPlatform, shader.Name, e.PsName, template.Name, shader.CurrentVariantValues),
                    InputLayout = inLayoutIndex < 0 ? new VertexType(VertexElement.Position3) : shader.Layouts[inLayoutIndex].ToVertexType(),
                    TargetFormats = shader.Layouts[outLayoutIndex].ToSurfaceFormats(),
                    SupportsInstancing = e.SupportsInstancing
                });
            }
            catch
            {
                throw new SemanticError(-1, "SC0007", "", string.Format("Failed to bind effect \"{0}\" declared in shader \"{1}\" (did you re-define an effect with the same name?).", e.Name, shader.Name));
            }
        }

        private string CleanHlslCode(string srcCode)
        {
            // code formatting
            srcCode = Regex.Replace(srcCode, "[ ]+", " "); // collapse multiple spaces
            srcCode = srcCode.Replace(" ;", ";");
            srcCode = srcCode.Replace(" . ", ".");
            srcCode = srcCode.Replace(" ( ", "(");
            srcCode = srcCode.Replace(" )", ")");
            srcCode = srcCode.Replace("return(", "return ("); // correct previous over-inclusive space removal
            return srcCode;
        }


        #region Shader programs namings

        /// <summary>
        /// Returns the name of the compiled version of a given Vertex Shader.
        /// </summary>
        /// <param name="platform">Platform for which the vs has been compiled.</param>
        /// <param name="shaderName">Name of the shader (as specified in the dfx file).</param>
        /// <param name="procedureName">Name of the vs procedure, can be retrived from the binding table.</param>
        /// <param name="templateName">Name of the parent template, an empty string can be passed for effect with no template.</param>
        /// <param name="variants">List of variant variable names paired with their value that defines the current shader version</param>
        /// <returns></returns>
        private static string GetProgramName(IGraphicsAPI platform, string shaderName, string procedureName, string templateName, Dictionary<string, string> variants)
        {
            return string.Format($"{platform.Description}_{shaderName}.{procedureName}{templateName}-{ShaderCompiler.GetShaderVariantID(variants)}");
        }

        /// <summary>
        /// Returns the file name of a shader source code, parsed for a given API.
        /// </summary>
        /// <param name="platform">Platform for which the shader will been compiled.</param>
        /// <param name="shaderName">Name of the shader (as specified in the dfx file).</param>
        /// <returns></returns>
        private static string GetShaderFileName(IGraphicsAPI platform, string shaderName, Dictionary<string, string> variants)
        {
            return string.Format("{0}_{1}-{2}.txt", platform.Description, shaderName, ShaderCompiler.GetShaderVariantID(variants));
        }


        #endregion

    }
}

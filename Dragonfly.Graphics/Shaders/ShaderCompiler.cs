using System;
using System.Collections.Generic;
using System.IO;
using Dragonfly.Utils;

namespace Dragonfly.Graphics.Shaders
{
    public abstract class ShaderCompiler
    {
        #region Static Shader Namings

        /// <summary>
        /// Returns the file name of the binding table for a give API.
        /// </summary>
        /// <returns></returns>
        public static string GetBindingTableFilename(IGraphicsAPI platform)
        {
            return string.Format("{0}_bindingTable.bin", platform.Description);
        }

        /// <summary>
        /// Returns the path of the binding table for a given API in a given resource folder.
        /// </summary>
        /// <returns></returns>
        public static string GetBindingTablePath(IGraphicsAPI platform, string resourceFolder)
        {
            return Path.Combine(GetShaderFolder(resourceFolder), GetBindingTableFilename(platform));
        }

        /// <summary>
        /// Returns the directory of the precompiled shaders for a given resource folder.
        /// </summary>
        /// <returns></returns>
        public static string GetShaderFolder(string resourceFolder)
        {
            return Path.Combine(resourceFolder, "shaders");
        }

        /// <summary>
        /// Returns an alphanumeric string that identify the specified shader variant.
        /// </summary>
        /// <param name="variants">List of variant variable names paired with their value that defines the current shader version</param>
        public static string GetShaderVariantID(Dictionary<string, string> variants)
        {
            int hash = 0;
            foreach (KeyValuePair<string, string> v in variants)
                hash = unchecked(hash + HashCode.Combine(HashCode.HashString(v.Key), HashCode.HashString(v.Value)));
            return hash.ToString("X");
        }

        public static readonly string INSTANCE_MATRIX_NAME = "INSTANCE_MATRIX";

        #endregion

        public InputBinding CreateBindingFromStream(ShaderBindingType type, BinaryReader reader)
        {
            // This code provides a default basic implementation for APIs that don't require additional binding data
            InputBinding binding = null;
            switch (type)
            {
                case ShaderBindingType.Constant:
                    binding = CreateConstantBinding();
                    break;
                case ShaderBindingType.Texture:
                    binding = CreateTextureBinding();
                    break;
            }

            if (binding != null) binding.Load(reader);
            return binding;
        }

        protected virtual ConstantBinding CreateConstantBinding()
        {
            return new ConstantBinding();
        }

        protected virtual TextureBinding CreateTextureBinding()
        {
            return new TextureBinding();
        }

        public virtual void Initialize() { }

        public abstract void BindInputs(ShaderSrcFile s, IInputBinder binder);

        public virtual void PreprocessShaderCode(ref string shaderCode) { }

        public abstract void SetGlobals(List<ShaderSrcFile.ConstantInfo> globalConsts, List<ShaderSrcFile.TextureInfo> globalTextures, IInputBinder binder);

        /// <summary>
        /// Generate the code to sample a specified texture, given a code expression representing the texture coordinates as parameter.
        /// </summary>
        public abstract string ProduceSamplingInstruction(ShaderSrcFile.TextureInfo tex, string texCoordsExpr);

        /// <summary>
        /// Generate the code to sample a specified texture, given a code expression representing the texture coordinates as parameter.
        /// <para/> The mipmap to be sampled is also specified by the user with an expression.
        /// </summary>
        public abstract string ProduceSamplingInstruction(ShaderSrcFile.TextureInfo tex, string texCoordsExpr, string lodExpr);

        /// <summary>
        /// Given a texture description, generate an instruction that retrieve the size of its texel as a 2d vector.
        /// </summary>
        public abstract string ProduceTexelSizeInstruction(ShaderSrcFile.TextureInfo tex);

        public abstract string ProduceInputDeclarations(ShaderSrcFile s, IBindingTable bindings);

        /// <summary>
        /// Returns the modifier code to be used to specify a global constant value in shader code.
        /// </summary>
        /// <returns></returns>
        public abstract string ProduceConstModifier();

        /// <summary>
        /// Returns the type-name declaration for a texture typed function parameter.
        /// </summary>
        public abstract string ProduceTextureParamDecl(string paramName);

        /// <summary>
        /// Returns the expression to be used to pass a texture typed parameter during a function call, given the texture name.
        /// </summary>
        public abstract string ProduceTextureParamUsage(ShaderSrcFile.TextureInfo texParameter);

        public abstract byte[] CompileShader(string source, ShaderSrcFile.ProgramInfo programInfo);

        public abstract string GenerateDebugInfo(byte[] compiledProgram);

        public bool OptimizationsEnabled { get; set; }

        /// <summary>
        /// Returns a shader layout struct declaration code, given its elements.
        /// </summary>
        public abstract string ProduceLayoutDecl(ShaderSrcFile.LayoutInfo layout);

        public virtual string ProduceFunctionCode(DFXShaderCompiler.FuncDeclaration funcDecl)
        {
            return $"{funcDecl.Type} {funcDecl.Name} ({funcDecl.ArgListCode})\n{{\n {funcDecl.Body} \n}}";
        }
    }
}

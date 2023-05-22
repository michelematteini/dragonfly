using Dragonfly.Graphics.Resources;
using Dragonfly.Graphics.Shaders;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Common
{
    public delegate void SetGlobalTextureCall<TArgs>(Shader toShader, string paramName, Texture value, TArgs args);
    public delegate void SetGlobalTargetCall<TArgs>(Shader toShader, string paramName, RenderTarget value, TArgs args);

    /// <summary>
    /// Simulates global textures by storing user bindings and propagating them to a shader when its changed
    /// </summary>
    internal class GlobalTexManager<TArgs>
    {    
        private Dictionary<string, List<string>> globalTexByShader; // shader name -> list of global texture param names
        private ShaderBindingTable bindingTable;
        private SetGlobalTextureCall<TArgs> setGlobalTexture;
        private SetGlobalTargetCall<TArgs> setGlobalTarget;


        public GlobalTexManager(ShaderBindingTable bindingTable, SetGlobalTextureCall<TArgs> setGlobalTexture, SetGlobalTargetCall<TArgs> setGlobalTarget)
        {
            this.bindingTable = bindingTable;
            this.setGlobalTexture = setGlobalTexture;
            this.setGlobalTarget = setGlobalTarget;
            this.globalTexByShader = new Dictionary<string, List<string>>();
            MainContext = new Context(this);

            // foreach shader
            foreach (string shaderName in bindingTable.GetAllShaderNames())
            {
                List<string> shaderGlobalTexList = new List<string>();

                // foreach input binding of the shader
                foreach (InputBinding input in bindingTable.GetAllShaderInputs(shaderName))
                {
                    // skip everything but global textures...
                    TextureBinding tex = input as TextureBinding;
                    if (tex == null || !tex.IsGlobal)
                        continue;

                    shaderGlobalTexList.Add(tex.Name); // save texture name / shader association
                }

                if (shaderGlobalTexList.Count > 0)
                    globalTexByShader[shaderName] = shaderGlobalTexList;
            }
        }

        public Context MainContext { get; private set; }

        internal class Context
        {
            private GlobalTexManager<TArgs> manager;
            private Context parent;
            private Dictionary<string, Pair<Texture, RenderTarget>> globalTexTable; // texture param name -> bindinded resource (either a texture or a rt)

            public Context(GlobalTexManager<TArgs> manager, Context parent = null)
            {
                this.manager = manager;
                this.parent = parent;
                globalTexTable = new Dictionary<string, Pair<Texture, RenderTarget>>();
            }

            private bool TryGetTexture(string name, out Pair<Texture, RenderTarget> value)
            {
                if (globalTexTable.TryGetValue(name, out value))
                    return true;

                if (parent == null)
                    return false;

                return parent.TryGetTexture(name, out value);
            }

            /// <summary>
            /// Set all binded global textures to the specified shader
            /// </summary>
            public void UpateShader(Shader targetShader, TArgs args)
            {
                List<string> globalTextList;
                if (manager.globalTexByShader.TryGetValue(manager.bindingTable.GetParentShaderName(targetShader.EffectName), out globalTextList))
                {
                    foreach (string globalTexName in globalTextList)
                    {
                        Pair<Texture, RenderTarget> globalTex;
                        
                        if (!TryGetTexture(globalTexName, out globalTex))
                            continue; // stil not initialized by the user

                        if (globalTex.First != null)
                            manager.setGlobalTexture(targetShader, globalTexName, globalTex.First, args);
                        else
                            manager.setGlobalTarget(targetShader, globalTexName, globalTex.Second, args);
                    }
                }
            }

            public void SetGlobalTexture(string name, Texture value)
            {
                globalTexTable[name] = new Pair<Texture, RenderTarget>(value, null);
            }

            public void SetGlobalTexture(string name, RenderTarget value)
            {
                globalTexTable[name] = new Pair<Texture, RenderTarget>(null, value);
            }

            public void MergeToParent()
            {
                if (parent == null)
                    return;

                foreach (var globalTex in globalTexTable)
                    parent.globalTexTable[globalTex.Key] = globalTex.Value;
            }

            public Context CreateChild()
            {
                return new Context(manager, this);
            }

            public void Reset()
            {
                globalTexTable.Clear();
            }

        }

    }

}

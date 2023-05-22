using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Shaders;
using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx9
{
    public class Directx9ShaderBinder : IShaderBinder
    {
        private Dictionary<string, int> globalBinds;
        private ShaderBindingTable bindingTable;

        // reg offsets
        private int nextGlobalC, nextGlobalI, nextGlobalB;
        private int offsetC, offsetI, offsetB;

        public void Initialize(Dictionary<string, ShaderInfo> shaders, ShaderBindingTable bindingTable)
        {
            this.bindingTable = bindingTable;

            // calculate global offsets
            globalBinds = new Dictionary<string, int>();
            foreach (ShaderInfo s in shaders.Values)
            {
                foreach (string c in s.Constants)
                {
                    Directx9Constant dxc = new Directx9Constant(c);
                    if (dxc.IsGlobal)
                    {
                        if (dxc.RegisterType == "c") offsetC += dxc.RegisterSize();
                        else if (dxc.RegisterType == "b") offsetB += dxc.RegisterSize();
                        else if (dxc.RegisterType == "i") offsetI += dxc.RegisterSize();
                    }
                }
            }
        }

        public void BindConstants(ShaderInfo shader)
        {
            //Constant string format:
            //[global] <Type> <Name>;
            int cx = offsetC, bx = offsetB, ix = offsetI;
            for (int i = 0; i < shader.Constants.Count; i++)
            {
                int reg = 0;

                // constant parsing
                Directx9Constant constant = new Directx9Constant(shader.Constants[i]);

                // binding       
                if (constant.IsGlobal)
                {
                    if (globalBinds.ContainsKey(constant.Name))
                    {
                        // global already binded
                        reg = globalBinds[constant.Name];
                    }
                    else
                    {
                        // bind a new global
                        switch (constant.RegisterType)
                        {
                            case "c": reg = nextGlobalC; nextGlobalC += constant.RegisterSize(); break;
                            case "b": reg = nextGlobalB; nextGlobalB += constant.RegisterSize(); break;
                            case "i": reg = nextGlobalI; nextGlobalI += constant.RegisterSize(); break;
                        }
                        globalBinds[constant.Name] = reg;
                    }
                }
                else
                {
                    // local constant, save register and increment pointer to the next free position
                    switch(constant.RegisterType)
                    {
                        case "c": reg = cx; cx += constant.RegisterSize(); break;
                        case "b": reg = bx; bx += constant.RegisterSize(); break;
                        case "i": reg = ix; ix += constant.RegisterSize(); break;
                    }
                }

                shader.Constants[i] = string.Format("{0} {1}{2}: register({4}{3});", constant.Type, constant.Name, constant.IsArray ? ("[" + constant.ArraySize + "]") : "", reg, constant.RegisterType);
                ShaderBinding constBinding = new ShaderBinding(shader.Name, constant.Name, constant.Type, reg, ShaderBindingType.Constant);
                bindingTable[constBinding.ShaderName, constBinding.Name] = constBinding;
            }
        }

        public void BindTextures(ShaderInfo shader)
        {
            //Texture string format:
            //Texture<<TextureType>> <Name> : <SamplerOpt>, <SamplerOpt>, ...;
            //SamplerOpt is a value from enum 'TextureBindingOptions'	
            for (int i = 0; i < shader.Textures.Count; i++)
            {
                string[] declElems = shader.Textures[i].Trim().Split(new char[] { ' ', '\t', ';', ':', ',', '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
                shader.Textures[i] = string.Format("sampler {0}: register(s{1});", declElems[2], i);
                ShaderBinding texBinding = new ShaderBinding(shader.Name, declElems[2], declElems[1], i, ShaderBindingType.Texture);

                for (int pi = 3; pi < declElems.Length; pi++)
                {
                    if (declElems[pi].StartsWith("#"))
                    //bg color param
                    {
                        texBinding.TextureBorderColor = new Float3(declElems[pi]);
                    }
                    else
                    //other sampler state
                    {
                        TextureBindingOptions opt = (TextureBindingOptions)Enum.Parse(typeof(TextureBindingOptions), declElems[pi]);
                        texBinding.TexBindingOptions = texBinding.TexBindingOptions | opt;
                    }
                }

                bindingTable[texBinding.ShaderName, texBinding.Name] = texBinding;
            }
        }

        public void BindEffects(ShaderInfo shader)
        {
            for(int i = 0; i < shader.EffectNames.Count; i++) 
			{
                bindingTable.BindEffect(new EffectBinding(
                    shader.EffectNames[i],
                    shader.Name,
                    shader.VsNames[shader.Effects[i * 2]],
                    shader.PsNames[shader.Effects[i * 2 + 1]]
                ));
            }
        }

    }

}

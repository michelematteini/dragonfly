using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dragonfly.Graphics.Shaders
{
    public class ShaderInfo
    {
        public string Name;
        public string Code;
        public List<string> VsNames;
        public List<string> PsNames;
        public List<string> Constants;
        public List<string> Textures;
        public List<string> Includes;
        public List<int> Effects;//alternate indices from vs/pv name arrays
        public List<string> EffectNames;

        public static ShaderInfo CreateEmpty()
        {
            ShaderInfo si = new ShaderInfo();
            si.VsNames = new List<string>();
            si.PsNames = new List<string>();
            si.Constants = new List<string>();
            si.Textures = new List<string>();
            si.Includes = new List<string>();
            si.Effects = new List<int>();
            si.EffectNames = new List<string>();

            return si;
        }
        
        public static Dictionary<string, string> GenerateCode(List<ShaderInfo> shaders, IShaderBinder binder)
        {
            //Add an empty effects to give user a referce to shaders if none is defined
            for (int si = 0; si < shaders.Count; si++)
            {
                if (shaders[si].Effects.Count == 0)
                {
                    shaders[si].addEmptyEffect();
                }
            }

            //Clone shaders to be processed into  hashmaps
            Dictionary<string, ShaderInfo> solvedShaders = new Dictionary<string, ShaderInfo>();
            Dictionary<string, ShaderInfo> shadersMap = new Dictionary<string, ShaderInfo>();
            foreach (ShaderInfo s in shaders)
            {
                solvedShaders[s.Name] = s.Clone();
                shadersMap[s.Name] = s.Clone();
            }

            //Process Includes
            for (int si = 0; si < shaders.Count; si++)
            {
                solvedShaders[shaders[si].Name].processIncludes(shadersMap);
            }
			
			//Process and bind Constants		
            for (int si = 0; si < shaders.Count; si++)
            {
                binder.BindConstants(solvedShaders[shaders[si].Name]);
            }
			
			//Process and bind Textures
			for (int si = 0; si < shaders.Count; si++)
            {
                binder.BindTextures(solvedShaders[shaders[si].Name]);
            }

            //Generate shaders code
            Dictionary<string, string> scodes = new Dictionary<string, string>();
            for (int si = 0; si < shaders.Count; si++)
            {
				ShaderInfo s  = solvedShaders[shaders[si].Name];
				
				string code = string.Empty;	
				code += string.Join("\n", s.Constants);
                code += "\n";
				code += string.Join("\n", s.Textures);
				code += s.Code;
				//TODO: add other code chunks here
				
				scodes[s.Name] = code;
            }
			
			//Bind effects
			for (int si = 0; si < shaders.Count; si++)
            {
                binder.BindEffects(solvedShaders[shaders[si].Name]);
            }

			return scodes;
        }

        public ShaderInfo Clone()
        {
            ShaderInfo si = new ShaderInfo();
            si.Name = this.Name;
            si.Code = this.Code;
            si.VsNames = new List<string>(VsNames);
            si.PsNames = new List<string>(PsNames);
            si.Constants = new List<string>(Constants);
            si.Textures = new List<string>(Textures);
            si.Includes = new List<string>(Includes);
            si.Effects = new List<int>(Effects);
            si.EffectNames = new List<string>(EffectNames);
            return si;
        }

        //include a shader
        private void include(ShaderInfo shader)
        {
            this.Code = shader.Code + this.Code;
            this.Constants.AddRange(shader.Constants);
            this.Textures.AddRange(shader.Textures);
        }

        private void processIncludes(Dictionary<string, ShaderInfo> shaders)
        {
            HashSet<string> included = new HashSet<string>();//save included to prevent include loops
            included.Add(this.Name);
            this.subProcessIncludes(shaders, included, this.Name);
        }

        private void subProcessIncludes(Dictionary<string, ShaderInfo> shaders, HashSet<string> included, string curShaderName)
        {
            ShaderInfo si = shaders[curShaderName];
            //for each include, reversed cicle to respect inclusion order
            for (int i = si.Includes.Count - 1; i >= 0; i--)
            {
                //Check for include loops
                if (included.Contains(Includes[i])) continue;

                if (shaders.ContainsKey(si.Includes[i]))
                // include shader found
                {
                    ShaderInfo inShader = shaders[si.Includes[i]];
                    included.Add(inShader.Name);
                    this.include(inShader);
                    this.subProcessIncludes(shaders, included, inShader.Name);
                }
                else
                // invalid include directive
                {
                    throw new Exception(string.Format("shader '{0}' not found! (used by {1})", si.Includes[i], curShaderName));
                }      
            }
        }

        private void addEmptyEffect()
        {
            // create effect code
            StringBuilder effectTempl = new StringBuilder();
            effectTempl.AppendLine();
            effectTempl.AppendLine("struct VSOUT_{0} {{ float4 p : POSITION; }};");
            effectTempl.AppendLine("struct PSOUT_{0} {{ float4 c : COLOR0; }};");
            effectTempl.AppendLine("VSOUT_{0} VS_{0}() {{ return (VSOUT_{0})0; }}");
            effectTempl.AppendLine("PSOUT_{0} PS_{0}() {{ return (PSOUT_{0})0; }}");
            string effectCode = string.Format(effectTempl.ToString(), Name);

            // add empty effect
            Code += effectCode;
            VsNames.Add("VS_" + Name);
            PsNames.Add("PS_" + Name);
            EffectNames.Add(Name);
            Effects.Add(0);
            Effects.Add(0);
        }

    }
}

using Dragonfly.Graphics.Math;
using DSLManager.Parsing;
using System;
using System.Collections.Generic;
using Dragonfly.Utils;
using System.Text;

namespace Dragonfly.Graphics.Shaders
{

    public class ShaderSrcFile : IParsedCodeType
    {
        public struct TemplateInfo
        {
            public string Name;
            public string LayoutType;
        }


        public struct EffectInfo
        {
            public string Name, VsName, PsName;
            public bool SupportsInstancing;
            public bool IsTemplate;
            public List<TemplateInfo> Templates;

            public override string ToString() { return Name; }
        }

        public struct ConstantInfo
        {         
            public string Type, Name;
            public bool IsGlobal, IsArray, IsConstant, IsDynamic;
            public int ArraySize;

            public override string ToString() { return Type + " " + Name; }
        }
	
        public struct TextureInfo
        {
            public string Name, Type;
            public Float3 BorderColor;
            public TextureBindingOptions BindingOptions;
            public bool IsGlobal;
            public bool IsParameter;

            public override string ToString() { return "texture " + Name; }
        }

        public struct Variant
        {
            public string Name;
            public List<VariantValue> Values;
            public int ActiveValueIndex;

            public Variant(string name, string[] validValues)
            {
                Name = name;
                ActiveValueIndex = 0;
                Values = new List<VariantValue>();
                foreach(string value in validValues)
                {
                    VariantValue varValue = new VariantValue();
                    varValue.Name = value;
                    Values.Add(varValue);
                }
            }

            public VariantValue SelectedValue { get { return Values[ActiveValueIndex]; } }

            public override string ToString() { return Name + " {" + string.Join<VariantValue>(",", Values) + "}"; }
        }

        public struct VariantValue
        {
            public string Name;

            public override string ToString() { return Name; }
        }

        public struct LayoutInfo
        {
            public string Name;
            public List<LayoutElemInfo> Elements;

            public VertexType ToVertexType()
            {
                VertexElement[] vertElems = new VertexElement[Elements.Count];
                for (int i = 0; i < Elements.Count; i++)
                    vertElems[i] = Elements[i].ToVertexElement();
                return new VertexType(vertElems);
            }

            public SurfaceFormat[] ToSurfaceFormats()
            {
                SurfaceFormat[] formats = new SurfaceFormat[Elements.Count];
                foreach (LayoutElemInfo layoutElem in Elements)
                {
                    int surfaceID;
                    int.TryParse(layoutElem.SemanticName[layoutElem.SemanticName.Length - 1].ToString(), out surfaceID);
                    switch(layoutElem.Type)
                    {
                        case "color":
                            formats[surfaceID] = SurfaceFormat.Color;
                            break;
                        case "float":
                            formats[surfaceID] = SurfaceFormat.Float;
                            break;
                        case "float2":
                            formats[surfaceID] = SurfaceFormat.Float2;
                            break;
                        case "float4":
                            formats[surfaceID] = SurfaceFormat.Float4;
                            break;
                        case "half":
                            formats[surfaceID] = SurfaceFormat.Half;
                            break;
                        case "half2":
                            formats[surfaceID] = SurfaceFormat.Half2;
                            break;
                        case "half4":
                            formats[surfaceID] = SurfaceFormat.Half4;
                            break;
                    }
                }
                return formats;
            }

            public override string ToString() { return Name + " {" + string.Join(",", Elements) + "}"; }
        }

        public struct LayoutElemInfo
        {
            public string Type;
            public string SemanticName;
            public string Name;

            public VertexElement ToVertexElement()
            {
                bool isPosition = SemanticName == "POSITION" || SemanticName == "POSITION0";
                switch (Type)
                {
                    default:
                        System.Diagnostics.Debug.WriteLine(string.Format("Encountered an unknown vertex element, type={0}, semantic={1}", Type, SemanticName));
                        return VertexElement.Float3;

                    case "float": return VertexElement.Float;
                    case "float2": return isPosition ? VertexElement.Position2 : VertexElement.Float2;
                    case "float3": return isPosition ? VertexElement.Position3 : VertexElement.Float3;
                    case "float4": return isPosition ? VertexElement.Position4 : VertexElement.Float4;
                }
            }

            public override string ToString() { return SemanticName + "(" + Type + ")"; }
        }

        public enum ProgramType
        {
            VertexShader,
            PixelShader
        }

        public struct ProgramInfo
        {
            public string EntryPoint, FromLayout, ToLayout;
            public bool SupportInstancing;
            public ProgramType Type;

            public override string ToString()
            {
                return string.Format("{3}{0}: {1}->{2}", EntryPoint, FromLayout, ToLayout, SupportInstancing ? "instanced " : "");
            }
        }


        public struct TemplateTypeInfo
        {
            public string FuncName;
            public string TypeDeclName;
            public string TypeImplName;
        }

        public struct StructInfo
        {
            public string Name;
            public string SrcCode;
        }

        public string Name;
        public string Body;
        public List<ProgramInfo> VSList;
        public List<ProgramInfo> PSList;
        public List<ConstantInfo> Constants;
        public List<TextureInfo> Textures;
        public List<string> Includes;
        public List<LayoutInfo> Layouts;
        public List<EffectInfo> Effects;
        public List<Variant> Variants;
        public List<TemplateTypeInfo> TemplateTypes;
        public List<StructInfo> Structs;
        public HashSet<string> ExternalTextures;

        public ShaderSrcFile()
        {
            VSList = new List<ProgramInfo>();
            PSList = new List<ProgramInfo>();
            Constants = new List<ConstantInfo>();
            Textures = new List<TextureInfo>();
            Includes = new List<string>();
            Effects = new List<EffectInfo>();
            Variants = new List<Variant>();
            Layouts = new List<LayoutInfo>();
            TemplateTypes = new List<TemplateTypeInfo>();
            Structs = new List<StructInfo>();
            ExternalTextures = new HashSet<string>();
        }  

        public ShaderSrcFile Clone()
        {
            ShaderSrcFile si = new ShaderSrcFile();
            si.Name = this.Name;
            si.Body = this.Body;
            si.VSList = new List<ProgramInfo>(VSList);
            si.PSList = new List<ProgramInfo>(PSList);
            si.Constants = new List<ConstantInfo>(Constants);
            si.Textures = new List<TextureInfo>(Textures);
            si.Includes = new List<string>(Includes);
            si.Effects = new List<EffectInfo>(Effects);
            si.Variants = new List<Variant>(Variants);
            si.Layouts = new List<LayoutInfo>(Layouts);
            si.TemplateTypes = new List<TemplateTypeInfo>(TemplateTypes);
            si.Structs = new List<StructInfo>(Structs);
            si.ExternalTextures = new HashSet<string>(ExternalTextures);
            return si;
        }

        /// <summary>
        /// Add code, declarations and effects from the specified file description.
        /// </summary>
        public void MergeInclude(ShaderSrcFile other)
        {
            Body = other.Body + Body;
            Constants.AddRange(other.Constants);
            Textures.AddRange(other.Textures);
            VSList.AddRange(other.VSList);
            PSList.AddRange(other.PSList);
            Effects.AddRange(other.Effects);
            Variants.AddRange(other.Variants);
            Layouts.AddRange(other.Layouts);
            Structs.InsertRange(0, other.Structs);
            ExternalTextures.UnionWith(other.ExternalTextures);

            // merge template type definitions
            foreach(TemplateTypeInfo typeInfo in other.TemplateTypes)
            {
                MergeTemplateType(typeInfo);
            }

            // solve external textures
            foreach(TextureInfo tex in Textures)
            {
                ExternalTextures.Remove(tex.Name);
            }
        }

        public void MergeTemplateType(TemplateTypeInfo typeInfo)
        {
            int index = TemplateTypes.FindIndex((t) => t.FuncName == typeInfo.FuncName);
            if (index < 0)
            {
                TemplateTypes.Add(typeInfo);
            }
            else
            {
                TemplateTypeInfo mergedType;
                mergedType.FuncName = typeInfo.FuncName;
                mergedType.TypeDeclName = string.IsNullOrEmpty(typeInfo.TypeDeclName) ? TemplateTypes[index].TypeDeclName : typeInfo.TypeDeclName;
                mergedType.TypeImplName = string.IsNullOrEmpty(TemplateTypes[index].TypeImplName) ? typeInfo.TypeImplName : TemplateTypes[index].TypeImplName;
                TemplateTypes[index] = mergedType;
            }
        }

        public void MergeAllIncludes(List<ShaderSrcFile> allShaders)
        {
            foreach (ShaderSrcFile included in GetRecursiveIncludeIterator(allShaders))
                MergeInclude(included);
        }

        public IEnumerable<ShaderSrcFile> GetRecursiveIncludeIterator(List<ShaderSrcFile> allShaders)
        {
            // index shaders by name
            Dictionary<string, ShaderSrcFile> shaderMap = new Dictionary<string, ShaderSrcFile>();
            for (int i = 0; i < allShaders.Count; i++)
                shaderMap[allShaders[i].Name] = allShaders[i];

            // retrieve all shaders recursively included by this shader
            HashSet<string> toBeIncluded = new HashSet<string>();
            {
                Stack<ShaderSrcFile> toBeProcessed = new Stack<ShaderSrcFile>();
                toBeProcessed.Push(this);

                while (toBeProcessed.Count > 0)
                {
                    ShaderSrcFile curShader = toBeProcessed.Pop();
                    if (toBeIncluded.Contains(curShader.Name)) continue;
                    if (curShader.Name != this.Name) toBeIncluded.Add(curShader.Name); // add include

                    foreach (string includeName in curShader.Includes)
                    {
                        if(!shaderMap.ContainsKey(includeName))
                        {
                            Console.WriteLine(string.Format("Shader '{0}' not found! (used by shader {1})", includeName, curShader.Name));
                            continue;
                        }

                        toBeProcessed.Push(shaderMap[includeName]); // add sub-includes to process queue
                    }
                }
            }

            // sort includes by required order
            List<string> sortedIncludes = new List<string>();
            {
                while (toBeIncluded.Count > 0)
                {
                    foreach (string includeName in toBeIncluded)
                    {
                        bool requiredShadersIncluded = true;
                        foreach (string subInclude in shaderMap[includeName].Includes)
                        {
                            if (toBeIncluded.Contains(subInclude))
                            {
                                requiredShadersIncluded = false;
                                break;
                            }
                        }

                        if (requiredShadersIncluded)
                        {
                            // all shaders required by the current one have been resolved, include current
                            sortedIncludes.Add(includeName);
                            toBeIncluded.Remove(includeName);
                            break;
                        }
                    }
                }
            }

            // yield includes in the correct order
            for (int i = sortedIncludes.Count - 1; i >= 0; i--)
                yield return shaderMap[sortedIncludes[i]];

        }

        public override string ToString()
        {
            return Name + (Variants.Count == 0 ? string.Empty : " ( Variant : " + ShaderCompiler.GetShaderVariantID(CurrentVariantValues) + ")");
        }

        public string GetSrcCode()
        {
            return Body;
        }

        public int VariantCount
        {
            get
            {
                int varCount = 1;
                foreach (Variant v in Variants)
                    varCount *= v.Values.Count;
                return varCount;
            }
        }

        public Dictionary<string, string> CurrentVariantValues
        {
            get
            {
                Dictionary<string, string> varValues = new Dictionary<string, string>();
                foreach (Variant v in Variants)
                    varValues[v.Name] = v.SelectedValue.Name;
                return varValues;
            }
        }

        internal List<ShaderSrcFile> UnrollAllVariants()
        {
            List<ShaderSrcFile> variants = new List<ShaderSrcFile>();

            // skip unrolling if no variants are available
            if (Variants.Count == 0)
            {
                variants.Add(this.Clone());
                return variants;
            }

            for(int i = 0; i < VariantCount; i++)
            {
                ShaderSrcFile curVariation = Clone();

                // assign variant state
                for(int vi = 0, varIndex = i; vi < curVariation.Variants.Count; vi++)
                {
                    Variant v = curVariation.Variants[vi];
                    v.ActiveValueIndex = varIndex % v.Values.Count;
                    varIndex /= v.Values.Count;
                    curVariation.Variants[vi] = v;
                }

                variants.Add(curVariation);
            }

            return variants;
        }

        public void ApplyCurrentVariant()
        {
            StringBuilder variantHeader = new StringBuilder();
            variantHeader.AppendLine("#define False 0");
            variantHeader.AppendLine("#define True 1");

            HashSet<String> definedVariantValues = new HashSet<string>();

            int vid = 0;
            foreach (Variant v in Variants)
            {
                // add a define for each variant value
                foreach (VariantValue value in v.Values)
                {
                    if (value.Name == "True" || value.Name == "False")
                        continue; // globally defined above

                    if (definedVariantValues.Add(value.Name))
                        variantHeader.AppendFormatLine("#define {0} {1}", value.Name, vid++);
                }

                // add a define that fix the variant value
                if (v.SelectedValue.Name != "False")
                    variantHeader.AppendFormatLine("#define {0} {1}", v.Name, v.SelectedValue.Name);
            }
            Body = variantHeader + Body;
        }
    }
}

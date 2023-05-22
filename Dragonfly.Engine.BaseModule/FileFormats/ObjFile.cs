using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Dragonfly.BaseModule
{
    internal class ObjFile : ILoadedFileHandler
    {
        private static char[] objNewLineChars = new char[] { '\n', '\r' };
        private static char[] objValueSeparatorChars = new char[] { ' ', '\t' };
        private static char[] objVertexElemsSeparator = new char[] { '/' };

        // parser state
        private Dictionary<MutableStringRange, Action<MutableStringRange>> commands;
        private Action<ObjFile> onLoadingComplete;

        // obj file loading state
        private bool objLoaded;
        private int mtlLeftToLoad;
        private MutableString objFileContent;
        private MutableString mtlFilesContent;

        // obj parsing state
        private ObjGroup curGroup;
        private int lastGrpDefIndex;
        private int nextGrpID;
        private List<ObjVertex> faceCache;

        // parsed state
        public List<Float3> Vertices { get; private set; }

        public List<Float2> TexCoords { get; private set; }

        public List<Float3> Normals { get; private set; }

        public List<ObjGroup> Groups { get; private set; }

        public List<ObjMaterial> Materials { get; private set; }

        public ObjMaterial LastMaterial { get { return Materials[Materials.Count - 1]; } }

        public bool Loaded { get; private set; }

        public string FilePath { get; private set; }

        public ObjFile()
        {
            MutableString cmdNameBuff = new MutableString();

            //prepare parser
            commands = new Dictionary<MutableStringRange, Action<MutableStringRange>>();
            
            // material commands
            commands[cmdNameBuff.AppendAsRange("newmtl")] = Parse_newmtl;
            commands[cmdNameBuff.AppendAsRange("ka")] = args => LastMaterial.AmbientColor = Parse_Float3(args);
            commands[cmdNameBuff.AppendAsRange("kd")] = args => LastMaterial.DiffuseColor = Parse_Float3(args);
            commands[cmdNameBuff.AppendAsRange("ks")] = args => LastMaterial.SpecularColor = Parse_Float3(args);
            commands[cmdNameBuff.AppendAsRange("ns")] = args => LastMaterial.SpecularCoefficient = args.ToFloat();
            commands[cmdNameBuff.AppendAsRange("d")] = args => LastMaterial.Transparency = args.ToFloat();
            commands[cmdNameBuff.AppendAsRange("tr")] = args => LastMaterial.Transparency = args.ToFloat();
            commands[cmdNameBuff.AppendAsRange("illum")] = args => LastMaterial.IlluminationModel = args.ToInt();
            commands[cmdNameBuff.AppendAsRange("map_ka")] = args => LastMaterial.AmbientTextureMap = args.ToString();
            commands[cmdNameBuff.AppendAsRange("map_kd")] = args => LastMaterial.DiffuseTextureMap = args.ToString();
            commands[cmdNameBuff.AppendAsRange("map_ks")] = args => LastMaterial.SpecularTextureMap = args.ToString();
            commands[cmdNameBuff.AppendAsRange("map_ns")] = args => LastMaterial.SpecularHighlightTextureMap = args.ToString();
            commands[cmdNameBuff.AppendAsRange("map_d")] = args => LastMaterial.AlphaTextureMap = args.ToString();
            commands[cmdNameBuff.AppendAsRange("map_bump")] = args => LastMaterial.BumpMap = args.ToString();
            commands[cmdNameBuff.AppendAsRange("bump")] = args => LastMaterial.BumpMap = args.ToString();
            commands[cmdNameBuff.AppendAsRange("disp")] = args => LastMaterial.DisplacementMap = args.ToString();
            commands[cmdNameBuff.AppendAsRange("decal")] = args => LastMaterial.StencilDecalMap = args.ToString();
            commands[cmdNameBuff.AppendAsRange("norm")] = args => LastMaterial.NormalMap = args.ToString();
            commands[cmdNameBuff.AppendAsRange("cull")] = args => LastMaterial.Cull = args.ToString();
            commands[cmdNameBuff.AppendAsRange("map_Pr")] = args => LastMaterial.RoughnessMap = args.ToString();

            // object commands
            commands[cmdNameBuff.AppendAsRange("v")] = args => Vertices.Add(Parse_Float3(args));
            commands[cmdNameBuff.AppendAsRange("vt")] = args => TexCoords.Add(Parse_Float2(args));
            commands[cmdNameBuff.AppendAsRange("vn")] = args => Normals.Add(Parse_Float3(args));
            commands[cmdNameBuff.AppendAsRange("usemtl")] = Parse_usemtl;
            commands[cmdNameBuff.AppendAsRange("g")] = Parse_g;
            commands[cmdNameBuff.AppendAsRange("f")] = Parse_f;

            // prepare parsed state
            Vertices = new List<Float3>();
            TexCoords = new List<Float2>();
            Normals = new List<Float3>();
            Groups = new List<ObjGroup>();
            Materials = new List<ObjMaterial>();
            curGroup = new ObjGroup();
            curGroup.Name = "default" + nextGrpID;
            Groups.Add(curGroup);
            faceCache = new List<ObjVertex>();
        }

        public void LoadFromFile(string objFilePath, Action<ObjFile> onLoadingComplete)
        {
            this.onLoadingComplete = onLoadingComplete;
            AsyncFileLoader.LoadFile(objFilePath, this, false);
            FilePath = objFilePath;
        }

        public void OnFileLoaded(int requestID, string filePath, string loadedText)
        {
            if (!objLoaded) // === OBJ FILE LOADED
            {
                objLoaded = true;
                objFileContent = new MutableString(loadedText);
                
                // search for material libraries
                List<string> matLibraries = new List<string>();
                MutableStringRange objStream = objFileContent.FullRange;
                do
                {
                    MutableStringRange objLine = objStream.SplitAt(objNewLineChars, out objStream).Trim();

                    if (objLine.Size == 0 || !objLine.StartsWith("mtllib "))
                        continue;

                    MutableStringRange matlibName;
                    objLine.SplitAt(objValueSeparatorChars, out matlibName);
                    matLibraries.Add(matlibName.ToString());

                } while (objStream.Size > 0);

                if (matLibraries.Count > 0)
                {
                    // start loading mtls 
                    mtlFilesContent = new MutableString("");
                    mtlLeftToLoad = matLibraries.Count;
                    string mtlDir = Path.GetDirectoryName(filePath);
                    for (int i = 0; i < matLibraries.Count; i++)
                        matLibraries[i] = Path.Combine(mtlDir, matLibraries[i]);
                    AsyncFileLoader.LoadAllFiles(matLibraries.ToArray(), this, false);
                }
                else
                {
                    ParseLines();
                }
            }
            else // === MTL FILE LOADED
            {
                mtlFilesContent.Append(loadedText).AppendLine();
                mtlLeftToLoad--;
                if (mtlLeftToLoad == 0) 
                    ParseLines();
            }
        }

        private void ParseLines()
        {
            // parse materials
            MutableStringRange mtlStream = mtlFilesContent.FullRange;
            do
            {
                MutableStringRange mtlLine = mtlStream.SplitAt(objNewLineChars, out mtlStream).Trim();

                if (mtlLine.Size == 0 || mtlLine.StartsWith('#'))
                    continue;

                MutableStringRange args;
                MutableStringRange cmdName = mtlLine.SplitAt(objValueSeparatorChars, out args).ToLower();
                if (commands.ContainsKey(cmdName))
                    commands[cmdName](args);

            } while (mtlStream.Size > 0);

            // parse obj
            MutableStringRange objStream = objFileContent.FullRange;
            do
            {
                MutableStringRange objLine = objStream.SplitAt(objNewLineChars, out objStream).Trim();

                if (objLine.Size == 0 || objLine.StartsWith('#'))
                    continue;

                MutableStringRange args;
                MutableStringRange cmdName = objLine.SplitAt(objValueSeparatorChars, out args).ToLower();
                if (commands.ContainsKey(cmdName))
                    commands[cmdName](args);

            } while (objStream.Size > 0);

            // free memory and signal completion
            Loaded = true;
            objFileContent = null;
            mtlFilesContent = null;
            if (onLoadingComplete != null)
            {
                onLoadingComplete(this);
                onLoadingComplete = null;
            }
        }

        private string GetObjArg(string line)
        {
            int argIndex = line.IndexOf(' ');
            return line.Substring(argIndex + 1);
        }

        private string GetObjCommand(string line)
        {
            int argIndex = line.IndexOf(' ');
            if (argIndex < 0) return line.ToLower();
            return line.Substring(0, argIndex).ToLower();
        }

        private void CleanupLines(string[] lines, List<string> cleaned)
        {
            // clean up lines
            for (int i = 0; i < lines.Length; i++)
            {
                string cleanLine = lines[i].Trim();
                if (cleanLine.Length == 0 /*empty line*/ || cleanLine[0] == '#' /*comment*/) continue;
                cleaned.Add(cleanLine);
            }
        }

        #region STDs

        private void Parse_newmtl(MutableStringRange args)
        {
            Materials.Add(new ObjMaterial(args.ToString()));
        }

        private Float3 Parse_Float3(MutableStringRange args)
        {
            MutableStringRange xStr = args.SplitAt(objValueSeparatorChars, out args);
            MutableStringRange yStr = args.SplitAt(objValueSeparatorChars, out args);
            MutableStringRange zStr = args;
            return new Float3(xStr.ToFloat(), yStr.ToFloat(), zStr.ToFloat());
        }

        private Float2 Parse_Float2(MutableStringRange args)
        {
            MutableStringRange xStr = args.SplitAt(objValueSeparatorChars, out args);
            MutableStringRange yStr = args;
            return new Float2(xStr.ToFloat(), yStr.ToFloat());
        }

        private void Parse_f(MutableStringRange args)
        {
            faceCache.Clear();
            do
            {
                MutableStringRange vertexStr = args.SplitAt(objValueSeparatorChars, out args).Trim();

                if (vertexStr.Size > 0)
                {
                    ObjVertex v;
                    v.VertexIndex = vertexStr.SplitAt(objVertexElemsSeparator, out vertexStr).ToInt();
                    v.TexCoordIndex = vertexStr.SplitAt(objVertexElemsSeparator, out vertexStr).ToInt();
                    v.NormalIndex = vertexStr.SplitAt(objVertexElemsSeparator, out vertexStr).ToInt();
                    faceCache.Add(v);
                }
            } while (args.Size > 0);

            curGroup.Faces.Add(new ObjFace() { Vertices = faceCache.ToArray() });
        }

        private void Parse_usemtl(MutableStringRange args)
        {
            string matName = args.ToString();

            // search and reuse a compatible default or last declared group
            ObjGroup compatibleGrp = null;
            for (int i = lastGrpDefIndex; i < Groups.Count; i++)
            {
                if (Groups[i].Material != null && Groups[i].Material.Name == matName)
                {
                    compatibleGrp = Groups[i];
                    break;
                }
            }

            if (compatibleGrp != null)
                curGroup = compatibleGrp;
            else
            {
                if (curGroup.Faces.Count > 0)
                    CreateNewDefaultGroup();

                // search for material and assign it to the current group
                curGroup.Material = Materials.Find(m => m.Name.ToLower() == matName.ToLower());
            }
        }

        private void Parse_g(MutableStringRange args)
        {
            if (curGroup.Faces.Count > 0)
                CreateNewDefaultGroup();
            curGroup.Name = args.ToString();
            lastGrpDefIndex = Groups.Count - 1;
        }

        #endregion

        private void CreateNewDefaultGroup()
        {
            curGroup = new ObjGroup();
            curGroup.Name = "default" + (++nextGrpID);
            Groups.Add(curGroup);
        }

        public void OnFileLoaded(int requestID, string filePath, byte[] loadedBytes) { throw new NotImplementedException(); }
    }

    public struct ObjVertex
    {
        public int VertexIndex, TexCoordIndex, NormalIndex;
    }

    public struct ObjFace
    {
        public ObjVertex[] Vertices;
    }

    public class ObjGroup
    {
        public ObjGroup()
        {
            Faces = new List<ObjFace>();
        }

        public string Name;
        public List<ObjFace> Faces;
        public ObjMaterial Material;

        public override string ToString() { return Name; }
    }

    public class ObjMaterial
    {
        public ObjMaterial(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }

        public Float3 AmbientColor { get; set; }

        public Float3 DiffuseColor { get; set; }

        public Float3 SpecularColor { get; set; }

        public float SpecularCoefficient { get; set; }

        public float Transparency { get; set; }

        public int IlluminationModel { get; set; }

        public string AmbientTextureMap { get; set; }

        public string DiffuseTextureMap { get; set; }

        public string SpecularTextureMap { get; set; }

        public string SpecularHighlightTextureMap { get; set; }

        public string BumpMap { get; set; }

        public string DisplacementMap { get; set; }

        public string StencilDecalMap { get; set; }

        public string AlphaTextureMap { get; set; }

        public string NormalMap { get; set; }

        public string RoughnessMap { get; set; }

        public string Cull { get; set; }

        public override string ToString() { return Name; }

        /// <summary>
        /// Convert a material loaded from an obj to the engine standard material parameters.
        /// </summary>
        public MaterialDescription ToMaterialParams()
        {
            MaterialDescription m = MaterialDescription.Default;
            m.Albedo = DiffuseColor;
            m.AlbedoMapPath = DiffuseTextureMap;
            m.NormalMapPath = NormalMap;
            m.DoubleSided = Cull == "none";
            m.Roughness = (1.0f / SpecularCoefficient).Saturate();
            m.UseTransparencyFromAlbedo = (AlphaTextureMap == DiffuseTextureMap);
            return m;
        }

    }
    
}

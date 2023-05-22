using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Dragonfly.BaseModule
{
    internal class TextSpriteIndex
    {
        public Dictionary<string, FaceIndex> Faces { get; private set; }

        public FaceIndex DefaultFace { get; set; }

        public TextSpriteIndex()
        {
            Faces = new Dictionary<string, FaceIndex>();
        }

        public void AddToIndex(string fontFileContent)
        {
            XmlReader reader = XmlReader.Create(new StringReader(fontFileContent), new XmlReaderSettings() { IgnoreWhitespace = true, IgnoreComments = true }) ;

            FaceIndex curFace = null;
            FaceSizeInfo sizeInfo = new FaceSizeInfo();
            Dictionary<char, FaceCharInfo> chars = new Dictionary<char, FaceCharInfo>();

            while (reader.Read())
            {
                string nodeName = reader.Name.ToLower();

                switch (nodeName)
                {
                    case "font":
                        if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            sizeInfo.Chars = chars;
                            curFace.Sizes.Add(sizeInfo);
                            curFace.Sizes.Sort((a, b) => { return a.Size - b.Size; });
                        }
                        break;

                    case "info":
                        if(reader.NodeType == XmlNodeType.Element)
                        {
                            string faceName = reader.GetAttribute("face");
                            if (!Faces.ContainsKey(faceName))
                            {
                                curFace = new FaceIndex();
                                curFace.Name = faceName;
                                curFace.Sizes = new List<FaceSizeInfo>();
                                Faces[faceName] = curFace;
                            }
                            else
                                curFace = Faces[faceName];

                            DefaultFace = curFace;
                            sizeInfo.Size = int.Parse(reader.GetAttribute("size"));
                        }
                        break;

                    case "common":
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            sizeInfo.XOffset = int.Parse(reader.GetAttribute("xoffset"));
                            sizeInfo.YOffset = int.Parse(reader.GetAttribute("yoffset"));
                        }
                        break;

                    case "chars":
                        break;

                    case "char":
                        if(reader.NodeType == XmlNodeType.Element)
                        {
                            FaceCharInfo curChar = new FaceCharInfo();
                            curChar.X = int.Parse(reader.GetAttribute("x"));
                            curChar.Y = int.Parse(reader.GetAttribute("y"));
                            curChar.Width = int.Parse(reader.GetAttribute("width"));
                            curChar.Height = int.Parse(reader.GetAttribute("height"));
                            curChar.XOffset = int.Parse(reader.GetAttribute("xoffset"));
                            curChar.YOffset = int.Parse(reader.GetAttribute("yoffset"));
                            curChar.XAdvance = int.Parse(reader.GetAttribute("xadvance"));
                            char c = (char)int.Parse(reader.GetAttribute("id"));
                            chars[c] = curChar;
                        }
                        break;

                    case "kernings":
                        break;

                    case "kerning":
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            char c = (char)int.Parse(reader.GetAttribute("second"));
                            char previous = (char)int.Parse(reader.GetAttribute("first"));
                            int kAmmount = int.Parse(reader.GetAttribute("amount"));
                            chars[c].SetKerning(previous, kAmmount);
                        }
                        break;
                }


            }

            reader.Close();
        }

    }

    internal class FaceIndex
    {
        public string Name;
        public List<FaceSizeInfo> Sizes;    
        
        public FaceSizeInfo GetCloserSize(float preferredSize)
        {
            int i = Sizes.Count - 1;
            for (; i > 0 && preferredSize < 0.5f * (Sizes[i].Size  + Sizes[i - 1].Size); i--) ;
            return Sizes[i];
        }

    }

    internal struct FaceSizeInfo
    {
        public int Size;
        public int XOffset;
        public int YOffset;
        public Dictionary<char, FaceCharInfo> Chars;
    }

    internal struct FaceCharInfo
    {
        public int X, Y, Width, Height, XOffset, YOffset, XAdvance;
        public Dictionary<char, int> Kernings; // accessed with the characted that preceed this

        public int GetKerningFor(char c)
        {
            int kerning = 0;
            if (Kernings != null)
                Kernings.TryGetValue(c, out kerning);
            return kerning;
        }

        public void SetKerning(char c, int kerning)
        {
            if (Kernings == null)
                Kernings = new Dictionary<char, int>();
            Kernings[c] = kerning;
        }
    }


}

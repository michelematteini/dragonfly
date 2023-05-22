using Dragonfly.Utils;
using System;
using System.Xml.Serialization;

namespace Dragonfly.Engine.Procedural
{
    [XmlInclude(typeof(ProceduralTreeDescr))]
    public class ProceduralMeshDescription
    {
        private static readonly Random SeedGenerator = new Random();

        public static ProceduralMeshDescription LoadFromFile(string srcFilePath)
        {
            return XmlUtils.LoadObject<ProceduralMeshDescription>(srcFilePath);
        }

        internal ProceduralMeshDescription()
        {
            Reseed();
        }

        /// <summary>
        /// Randomization seed used to generate the model
        /// </summary>
        public int Seed;

        public ProceduralMeshDescription Clone()
        {
            return (ProceduralMeshDescription)MemberwiseClone();
        }

        public void SaveToFile(string filePath)
        {
            XmlUtils.SaveObject<ProceduralMeshDescription>(this, filePath);
        }

        public void Reseed()
        {
            Seed = SeedGenerator.Next();
        }
    }

}

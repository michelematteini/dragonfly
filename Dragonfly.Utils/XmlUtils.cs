using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Dragonfly.Utils
{
    public static class XmlUtils
    {
        public static void SaveObject<T>(T value, string xmlOutputFilePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            TextWriter writer = new StreamWriter(xmlOutputFilePath);
            serializer.Serialize(writer, value);
            writer.Close();
        }

        public static string ToXml<T>(T value)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringWriter writer = new StringWriter();
            serializer.Serialize(writer, value);
            string xml = writer.ToString();
            writer.Close();
            return xml;
        }

        public static T LoadObject<T>(string xmlInputPath)
        {
            FileStream fs = new FileStream(xmlInputPath, FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            object loadedValue = serializer.Deserialize(fs);
            fs.Close();
            return (T)loadedValue;
        }
    }

}

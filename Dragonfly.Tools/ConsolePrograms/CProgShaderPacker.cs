using Dragonfly.Utils;
using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace Dragonfly.Tools
{
    public class CProgShaderPacker : IConsoleProgram
    {

        public CProgShaderPacker()
        {

        }

        public string ProgramName { get { return "Shader Packer"; } }

        public void RunProgram()
        {
            Console.WriteLine("Insert the root directory of your module:");
            string moduleRoot = Console.ReadLine();

            if(!Directory.Exists(moduleRoot))
            {
                Console.WriteLine("The specified path must be one of an existing directory!");
                return;
            }

            string[] shaders = Directory.GetFiles(moduleRoot, "*.dfx", SearchOption.AllDirectories);

            if(shaders.Length == 0)
            {
                Console.WriteLine("No shader file found in the specified directory!");
                return;
            }

            foreach(string shaderPath in shaders)
            {
                Console.WriteLine("Shader found: " + Path.GetFileName(shaderPath));
            }

            SaveFileDialog saveDiag = new SaveFileDialog();
            saveDiag.Filter = "Shader package (*.dfp)|*.dfp";
            if (saveDiag.ShowDialog() != DialogResult.OK) return;
            

            using (ZipArchive modulePackage = ZipFile.Open(saveDiag.FileName, ZipArchiveMode.Create))
            {
                foreach (string shaderPath in shaders)
                {
                    modulePackage.CreateEntryFromFile(shaderPath, Path.GetFileName(shaderPath));
                }
            }
            
        }

        public static string Unpack(string modulePath)
        {
            string tempModuleFolder = Path.Combine(Path.GetTempPath(), "dfxunpack" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempModuleFolder);
            ZipFile.ExtractToDirectory(modulePath, tempModuleFolder);
            return tempModuleFolder;
        }

    }
}

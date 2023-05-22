using Dragonfly.Graphics;
using Dragonfly.Graphics.API.Directx9;
using Dragonfly.Graphics.Shaders;
using Dragonfly.Utils;
using DSLManager;
using DSLManager.Generation.Exporters;
using DSLManager.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dragonfly.Tools
{
    public class CProgShaderCompiler : IConsoleProgram
    {
        private string outputFolder = "ouput\\";
        private string rootFolder = ".\\";
        private List<string> additionalFolders;
        private IGraphicsAPI[] outApiList;

        public CProgShaderCompiler() : this(new Directx9API())
        {
        }

        public CProgShaderCompiler(params IGraphicsAPI[] outApiList)
        {
            additionalFolders = new List<string>();
            loadCofiguration();
            this.outApiList = outApiList;
            CompileAllShaders = true;
            LoopEnabled = true;
        }

        public string ProgramName
        {
            get
            {
                return string.Format("Shader Compiler {1}({0})", 
                    string.Join(",", Array.ConvertAll<IGraphicsAPI, string>(outApiList, x => x.Description)),
                    CompileAllShaders ? "[Compile all shader]" : "[Update single shader]"
                );
            }
        }

        /// <summary>
        /// If set to false, the program will ask the user which shader should be compiled.
        /// </summary>
        public bool CompileAllShaders { get; set; }

        /// <summary>
        /// If set to true, when the compilation process is over, ask the user if another compilation should be launched.
        /// </summary>
        public bool LoopEnabled { get; set; }

        public void RunProgram()
        {
            if (outApiList.Length == 0)
                return;

            // create shader compiling system
            FileExporter exporter = new FileExporter(); // exporter
            exporter.OutputDir = outputFolder;
            DSLDebug.Output = LogMessage; // output

            // ask user which shader to compile
            string shaderNameFilter = null;
            if (!CompileAllShaders)
            {
                LogMessage("Insert the name of the shader to be compiled:", DSLDebug.MsgType.Question);
                shaderNameFilter = Console.ReadLine();
            }

            // load-compile loop
            for(bool compileAgain = true; compileAgain; compileAgain = LoopEnabled && Console.ReadKey().KeyChar == 'y')
            {
                // compile all APIs
                foreach (IGraphicsAPI api in outApiList)
                {
                    DFXShaderCompiler shaderCompiler = CreateDFXCompiler(api, shaderNameFilter);// shader compiler
                    ConsoleCompiler cc = new ConsoleCompiler(shaderCompiler, exporter);
                    cc.LoadSources += onLoadSources;
                    cc.LoadFilesAndCompile();
                }

                // ask user if he wants to re-compile
                if (LoopEnabled)
                    LogMessage("Do you want to compile again? (Y / N)", DSLDebug.MsgType.Question);
            }
        }

        DFXShaderCompiler CreateDFXCompiler(IGraphicsAPI api, string shaderNameFilter)
        {
            DFXShaderCompiler shaderCompiler = new DFXShaderCompiler(api);
            shaderCompiler.NativeCompiler.OptimizationsEnabled = OptimizationsEnabled;

            if (!string.IsNullOrEmpty(shaderNameFilter))
            {
                // filter a single shader
                shaderCompiler.ShaderNameFilter = curName => curName.Equals(shaderNameFilter);

                // load the current table for this api
                string resourcePath = PathEx.GetFirstTopDir(PathEx.DefaultResourceFolder, outputFolder);
                string shaderTablePath = ShaderCompiler.GetBindingTablePath(api, resourcePath);
                shaderCompiler.InputBindingTable = new ShaderBindingTable(api, File.ReadAllBytes(shaderTablePath));
            }

            return shaderCompiler;
        }

        private void LogMessage(string message, DSLDebug.MsgType type)
        {
            switch (type)
            {
                case DSLDebug.MsgType.Success:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                case DSLDebug.MsgType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case DSLDebug.MsgType.Failure:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case DSLDebug.MsgType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case DSLDebug.MsgType.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case DSLDebug.MsgType.Question:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            if (type == DSLDebug.MsgType.InfoProgress)
                Console.Write(message);
            else
                Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void onLoadSources(ConsoleCompiler compiler)
        {
            compiler.AddSourcesFromFolder(rootFolder, true);

            foreach (string include in additionalFolders)
                compiler.AddSourcesFromFolder(include, true);
        }

        private void loadCofiguration()
        {
            // assign defaults
            outputFolder = ShaderCompiler.GetShaderFolder(PathEx.DefaultResourceFolder);
            rootFolder = Path.GetDirectoryName(getFirstFileMatch(AppDomain.CurrentDomain.BaseDirectory, "sln"));

            if (File.Exists("options.ini"))
            {
                string[] options = File.ReadAllLines("options.ini");
                foreach (string opt in options)
                {
                    if (opt.TrimStart().StartsWith("//")) continue; // skip comments

                    string[] optElems = opt.Split('=');
                    switch (optElems[0].Trim())
                    {
                        case "OUTPUT": outputFolder = optElems[1].Trim(); break;
                        case "ROOT": rootFolder = optElems[1].Trim(); break;
                        case "PROJECT_EXT": rootFolder = Path.GetDirectoryName(getFirstFileMatch(AppDomain.CurrentDomain.BaseDirectory, optElems[1].Trim())); break;
                        case "INCLUDE": additionalFolders.Add(optElems[1].Trim()); break;
                        case "MODULE": additionalFolders.Add(CProgShaderPacker.Unpack(optElems[1].Trim())); break;
                    }
                }
            }
            else
            {
                File.WriteAllText("options.ini", Strings.OPTIONS_FILE_HEADER);
            }
        }

        private static string getFirstFileMatch(string startDir, string ext)
        {
            DirectoryInfo curPath = new DirectoryInfo(startDir);
            while (curPath != null)
            {
                string[] csprojPath = Directory.GetFiles(curPath.FullName, "*." + ext, SearchOption.TopDirectoryOnly);
                if (csprojPath.Length > 0) return csprojPath[0];
                curPath = curPath.Parent;
            }

            return string.Empty;
        }

        public bool OptimizationsEnabled { get; set; }
    }
}

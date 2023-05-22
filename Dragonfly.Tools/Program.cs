using Dragonfly.Graphics.API.Directx11;
using Dragonfly.Graphics.API.Directx9;
using Dragonfly.Graphics.API.Directx12;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.Tools
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ConsoleSelectionLoop selectionLoop = new ConsoleSelectionLoop("Dragonfly Engine Tools");


            List<CProgShaderCompiler> compilers = new List<CProgShaderCompiler>();
            Action<CProgShaderCompiler> AddCompilerProgram = (CProgShaderCompiler c) =>
            {
                selectionLoop.AddProgram(c);
                compilers.Add(c);
            };

            AddCompilerProgram(new CProgShaderCompiler(new Directx9API())); // directx9 shader compiler
            AddCompilerProgram(new CProgShaderCompiler(new Directx11API())); // directx11 shader compiler
            AddCompilerProgram(new CProgShaderCompiler(new Directx12API())); // directx11 shader compiler
            AddCompilerProgram(new CProgShaderCompiler(new Directx9API(), new Directx11API(), new Directx12API())); // compile shader for all APIs 
            AddCompilerProgram(new CProgShaderCompiler(new Directx9API(), new Directx11API(), new Directx12API()) { CompileAllShaders = false }); // compile single shader for all APIs 
            selectionLoop.AddProgram(new CProgShaderOptimization(compilers)); // enable or disable shader optimizations
            selectionLoop.AddProgram(new CProgShaderPacker()); // shader packer to export a module shaders into one distributable file
            selectionLoop.AddProgram(new CProgEquirectToCubeHDR()); // convert an equirect hdr image to a baked radiance cubemap
            selectionLoop.AddProgram(new CProgBrdfLUT()); // generates the brdf lut for physical rendering
            selectionLoop.AddProgram(new CProgUpdateHdrEV()); // resave an hdr image with a different exposure
            selectionLoop.Start();
        }
        
    }
}

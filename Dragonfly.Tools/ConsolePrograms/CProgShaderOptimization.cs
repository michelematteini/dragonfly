using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.Tools
{
    public class CProgShaderOptimization : IConsoleProgram
    {
        private List<CProgShaderCompiler> compilers;

        public CProgShaderOptimization(List<CProgShaderCompiler> compilers)
        {
            this.compilers = compilers;
        }

        public string ProgramName => "Turn shader optimizations on or off";

        public void RunProgram()
        {
            // ask to turn optimizations ON or OFF
            Console.WriteLine(
                "Shader optimizations are {0}, do you want to turn them {1}? (Y / N)", 
                OnOffStr(compilers[0].OptimizationsEnabled), 
                OnOffStr(!compilers[0].OptimizationsEnabled)
            );

            // apply changes
            bool optEnabled = compilers[0].OptimizationsEnabled != (Console.ReadKey().KeyChar == 'y');
            foreach (CProgShaderCompiler c in compilers)
                c.OptimizationsEnabled = optEnabled;

            // log result
            Console.WriteLine("Shader optimizations are {0}.", OnOffStr(compilers[0].OptimizationsEnabled));
        }

        private string OnOffStr(bool value)
        {
            return value ? "ON" : "OFF";
        }

    }
}
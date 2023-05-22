using Dragonfly.Engine.Core;
using Dragonfly.BaseModule;
using Dragonfly.Utils;
using Dragonfly.Graphics.Math;

namespace Dragonfly.Tools
{
    public class CProgBrdfLUT: IConsoleProgram
    {
        private IRenderLoop renderLoop;
        private EngineContext context;

        public string ProgramName => "Generate GGX Brdf LUT";

        public void RunProgram()
        {
            // retrieve input-output file locations
            string outputLUTPath;
            if (!ConsoleUtils.AskOutPath(".png", out outputLUTPath))
                return;

            BakingUtils.CreateContextAndRenderLoop(out context, out renderLoop);

            // prepare baking pipeline
            CompBakerBrdfLUT lutBaker = new CompBakerBrdfLUT(context.Scene.Root, new Int2(256, 256));
            lutBaker.Baker.OnCompletion = lut => BakingUtils.SaveTargetToFile(context, lut[0], outputLUTPath, renderLoop);
            
            renderLoop.Play(); // loop syncrhronously until cubemap is ready and saved
            context.Release();
        }
    }
}

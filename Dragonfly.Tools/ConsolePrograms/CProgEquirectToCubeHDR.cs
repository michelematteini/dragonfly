using Dragonfly.Engine.Core;
using Dragonfly.BaseModule;
using Dragonfly.Utils;

namespace Dragonfly.Tools
{
    public class CProgEquirectToCubeHDR : IConsoleProgram
    {
        private IRenderLoop renderLoop;
        private EngineContext context;

        public string ProgramName => "HDR Equirect to Cubemap2D file.";

        public void RunProgram()
        {
            // retrieve input-output file locations
            string inputHdrPath, outputRadiancePath;
            if (!ConsoleUtils.AskInOutPath(".hdr", ".hdr", out inputHdrPath, out outputRadiancePath))
                return;

            // retrieve other baking params
            float rotation = ConsoleUtils.AskFloat("Specify an horizontal rotation offset in radiance", 0);
            float exposureMul = ConsoleUtils.AskFloat("Specify a linear exposure multiplier", 1.0f);
            CompMtlCube2DHdrMipmap.FilterType mipmapFiltering = ConsoleUtils.AskEnum<CompMtlCube2DHdrMipmap.FilterType>("Mipmap Filtering", CompMtlCube2DHdrMipmap.FilterType.Bilinear);
            int cubeEdgeSize = ConsoleUtils.AskInt("Specify the cube side edge resolution", 1024);

            BakingUtils.CreateContextAndRenderLoop(out context, out renderLoop);

            // prepare baking pipeline
            CompBakerEquirectToCube2D cubeBaker = new CompBakerEquirectToCube2D(context.Scene.Root, cubeEdgeSize, rotation, exposureMul);
            cubeBaker.Baker.OnCompletion = cube2DResult =>
            {
                // start mipmap baking 
                CompBakerCube2DMipmaps cubeMipmapsBaker = new CompBakerCube2DMipmaps(cubeBaker, cubeBaker.Baker.FinalPass.RenderBuffer, 8);
                cubeMipmapsBaker.Baker.OnCompletion = mipmappedResult =>
                {
                    if (mipmapFiltering == CompMtlCube2DHdrMipmap.FilterType.GGX)
                    {
                        // start filtering mimaps
                        CompBakerCube2DGGX ggxMipmapsBaker = new CompBakerCube2DGGX(cubeMipmapsBaker, cubeMipmapsBaker.Baker.FinalPass.RenderBuffer, 8);
                        ggxMipmapsBaker.Baker.OnCompletion = filteredResult => BakingUtils.SaveTargetToFile(context, filteredResult[0], outputRadiancePath, renderLoop);
                    }
                    else
                    {
                        BakingUtils.SaveTargetToFile(context, mipmappedResult[0], outputRadiancePath, renderLoop);
                    }
                };
            };
            
            cubeBaker.InputEnviromentMap.SetSource(inputHdrPath); // trigger baking start
            renderLoop.Play(); // loop syncrhronously until cubemap is ready and saved
            context.Release();
        }
                
    }
}

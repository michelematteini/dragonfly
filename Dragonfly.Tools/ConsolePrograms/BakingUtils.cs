using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.API.Directx11;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Dragonfly.Tools
{
    public static class BakingUtils
    {
        public static void CreateContextAndRenderLoop(out EngineContext context, out IRenderLoop renderLoop)
        {
            // initialize engine            
            GraphicsAPIs.SetDefault(new Directx11API());
            EngineParams engineParams = new EngineParams();
            engineParams.Target = new VirtualTarget(1, 1);
            engineParams.ResourceFolder = PathEx.DefaultResourceFolder;
            EngineContext bakingContext = EngineFactory.CreateContext(engineParams);
            BaseMod baseMod = new BaseMod();
            bakingContext.AddModule(baseMod);
            baseMod.Initialize(BaseMod.Usage.Unspecified);
            context = bakingContext;
            
            // create a synchronous render loop
            renderLoop = new SyncRenderLoop();
            renderLoop.FrameRequest += args => bakingContext.RenderFrame();
        }

        public static void SaveTargetToFile(EngineContext context, RenderTargetRef rt, string outputPath, IRenderLoop toBeStopped = null)
        {
            // request result snapshot, and save it when available.
            rt.GetValue().SaveSnapshot();
            new CompActionOnEvent(new CompEventRtSnapshotReady(context.Scene.Root, rt.GetValue()).Event, () =>
            {
                if (toBeStopped != null)
                    toBeStopped.Stop();

                if (Path.GetExtension(outputPath) == HdrFile.Extension)
                {
                    // save an hdr file
                    HdrFile radianceFile = new HdrFile(rt.GetValue().Width, rt.GetValue().Height);
                    rt.GetValue().GetSnapshotData<byte>(radianceFile.GetRGBEDataPtr());
                    radianceFile.Save(outputPath);
                }
                else
                {
                    // save a bitmap file
                    Bitmap renderedImg;
                    rt.GetValue().TryGetSnapshotAsBitmap(out renderedImg, true);
                    renderedImg.Save(outputPath, FormatFromExtension(Path.GetExtension(outputPath)));
                }
            });
        }

        private static ImageFormat FormatFromExtension(string extension)
        {
            switch (extension.ToLower())
            {
                case ".png": default: return ImageFormat.Png;
                case ".bmp": return ImageFormat.Bmp;
                case ".jpg": case ".jpeg": return ImageFormat.Jpeg;
                case ".tiff": return ImageFormat.Tiff;
            }

        }
    }
}

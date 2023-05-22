using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System.IO;

namespace Dragonfly.BaseModule
{
    public class UiRenderPassCanvas : IUiCanvas
    {
        public UiRenderPassCanvas(CompRenderPass targetPass)
        {
            TargetPass = targetPass;
            Coords = new CoordContext(this, targetPass.Context.GetModule<BaseMod>().Settings.UI.FontPixSize);
        }

        public CompRenderPass TargetPass { get; set; }

        public string MaterialClass { get { return TargetPass.MainClass; } }

        public Int2 PixelSize { get { return TargetPass.Resolution; } }

        public CoordContext Coords { get; private set; }
    }
}

using Dragonfly.Engine.Core;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Base material used to apply a given shader to an image.
    /// </summary>
    public abstract class CompMtlImageProc : CompMtlImage
    {
        public CompMtlImageProc(Component parent, ImgProcessingType type) : base(parent)
        {
            SetVariantValue("fx", "ImgProc" + type.ToString());
        }

        public override string EffectName { get { return "ImgProcessing"; } }
    }

    public enum ImgProcessingType
    {
        Copy,
        Heatmap
    }


}

using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    public class MtlModAlphaMasking : MaterialModule
    {
        public MtlModAlphaMasking(CompMaterial parentMaterial, CompTextureRef alphaMask) : base(parentMaterial)
        {
            if (alphaMask == null)
                throw new ArgumentException("the alpha mash cannot be null!");
            Map = alphaMask;
            Enabled = MakeParam(false);
            MonitoredParams.Add(Map); // If the material already use this texture, and added it to the monitored, this call will duplicate it.
                                      // Its however wanted since this module will have to monitor its instance even if the original is removed / changed.
        }

        public CompTextureRef Map { get; private set; }

        public CompMaterial.Param<bool> Enabled { get; private set; }

        protected override void UpdateAdditionalParams(Shader s)
        {
            s.SetParam("alphaThr", Enabled ? Context.GetModule<BaseMod>().Settings.GlobalAlphaTestTHR : -1.0f);
        }

        public void CopyFrom(MtlModAlphaMasking other)
        {
            Enabled.Value = other.Enabled;
            MonitoredParams.Remove(Map);
            Map = other.Map;
            MonitoredParams.Add(Map);
        }
    }
}

using System;

namespace Dragonfly.Graphics
{
    public class DFGraphicSettings
    {
        public IntPtr TargetControl { get; set; }

        public bool FullScreen { get; set; }

        public int PreferredWidth { get; set; }

        public int PreferredHeight { get; set; }

        public string ResourceFolder { get; set; }

        public bool HardwareAntiAliasing { get; set; }

        public DFGraphicSettings()
        {
            ResourceFolder = "";
        }
    }
}

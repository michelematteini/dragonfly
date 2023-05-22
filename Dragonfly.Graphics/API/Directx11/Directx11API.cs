using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Shaders;
using DragonflyGraphicsWrappers.DX11;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx11
{
    public class Directx11API : IGraphicsAPI
    {
        public string Description
        {
            get { return "Directx11"; }
        }

        public IDFGraphics CreateGraphics(DFGraphicSettings settings)
        {
            return new Directx11Graphics(settings, this);
        }

        public ShaderCompiler CreateShaderCompiler()
        {
            return new Directx11ShaderCompiler();
        }

        public bool IsSupported
        {
            get
            {
                return DF_Directx3D11.IsAvailable();
            }
        }

        public List<Int2> DefaultDisplayResolutions
        {
            get
            {
                return DirectxUtils.DisplayModeToResolutionList(DF_Directx3D11.GetDefaultDisplayModes());
            }
        }

        public override string ToString()
        {
            return Description;
        }
    }
}

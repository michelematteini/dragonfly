using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Shaders;
using DragonflyGraphicsWrappers.DX9;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx9
{
    public class Directx9API : IGraphicsAPI
    {
        public string Description
        {
            get { return "Directx9"; }
        }

        public IDFGraphics CreateGraphics(DFGraphicSettings factory)
        {
            return new Directx9Graphics(factory, this);
        }

        public ShaderCompiler CreateShaderCompiler()
        {
            return new Directx9ShaderCompiler();
        }

        public bool IsSupported
        {
            get
            {
                DF_Directx3D9 dx9 = new DF_Directx3D9();
                return dx9.IsAvailable();
            }
        }

        public List<Int2> DefaultDisplayResolutions
        {
            get
            {
                DF_Directx3D9 dx9 = new DF_Directx3D9();
                return DirectxUtils.DisplayModeToResolutionList(dx9.GetDisplayModes());
            }
        }

        public override string ToString()
        {
            return Description;
        }
    }
}

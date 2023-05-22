using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Shaders;
using DragonflyGraphicsWrappers.DX12;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx12
{
    public class Directx12API : IGraphicsAPI
    {
        public string Description
        {
            get { return "Directx12"; }
        }

        public bool IsSupported
        {
            get
            {
                return DF_Directx3D12.IsAvailable();
            }
        }

        public List<Int2> DefaultDisplayResolutions
        {
            get
            {
                return DirectxUtils.DisplayModeToResolutionList(DF_Directx3D12.GetDefaultDisplayModes());
            }
        }

        public IDFGraphics CreateGraphics(DFGraphicSettings settings)
        {
            return new Directx12Graphics(settings, this);
        }

        public ShaderCompiler CreateShaderCompiler()
        {
            return new Directx12ShaderCompiler();
        }

        public override string ToString()
        {
            return Description;
        }
    }
}

using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Shaders;
using System.Collections.Generic;

namespace Dragonfly.Graphics
{
    public interface IGraphicsAPI
    {
        string Description { get; }

        IDFGraphics CreateGraphics(DFGraphicSettings settings);

        ShaderCompiler CreateShaderCompiler();

        bool IsSupported { get; }

        List<Int2> DefaultDisplayResolutions { get; }
    }
}

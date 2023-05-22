using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Shaders
{
    public interface IShaderBinder
    {
        void Initialize(Dictionary<string, ShaderInfo> shaders, ShaderBindingTable bindingTable);

        void BindConstants(ShaderInfo shader);

        void BindTextures(ShaderInfo shader);

        void BindEffects(ShaderInfo shader);
    }
}

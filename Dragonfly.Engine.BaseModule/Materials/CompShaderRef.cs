using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Manage a shader resource and its loading. Can be used to update shader parameter marked as global for other shaders including them.
    /// </summary>
    public class CompShaderRef : Component<Shader>, ICompAllocator
    {
        private string requestedEffect;
        private Shader shader;

        public CompShaderRef(Component parent, string effectName) : base(parent)
        {
            EffectName = effectName;
        }

        public string EffectName
        {
            get
            {
                return requestedEffect;
            }
            set
            {
                requestedEffect = value;
                LoadingRequired = true;
            }
        }

        public bool LoadingRequired { get; private set; }

        public bool Available { get { return shader != null; } }

        public void LoadGraphicResources(EngineResourceAllocator g)
        {
            shader = g.CreateShader(EffectName, ShaderStates.Default);
            LoadingRequired = false;
        }

        public void ReleaseGraphicResources()
        {
            if (shader != null)
                shader.Release();
            LoadingRequired = true;
        }

        protected override Shader getValue()
        {
            return shader;
        }
    }
}

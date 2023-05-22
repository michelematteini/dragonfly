using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    public class RenderTargetRef
    {
        private CompRenderBuffer srcBuffer;
        private int targetIndex;

        public RenderTargetRef(CompRenderBuffer srcBuffer) : this(srcBuffer, 0) { }

        public RenderTargetRef(CompRenderBuffer srcBuffer, int targetIndex)
        {
            this.srcBuffer = srcBuffer;
            this.targetIndex = targetIndex;
        }

        public bool Available
        {
            get
            {
                return !srcBuffer.Disposed && !srcBuffer.LoadingRequired;
            }
        }

        public RenderTarget GetValue()
        {
            if (!Available) 
                return null;
            return srcBuffer[targetIndex];
        }

    }
}

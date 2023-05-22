using DragonflyGraphicsWrappers.DX11;

namespace Dragonfly.Graphics.API.Directx11
{
    internal struct CBufferInstance
    {
        public CBuffer CPUValue;
        public DF_Buffer11 GPUResource;

        public bool IsAvailable
        {
            get { return CPUValue != null; }
        }
    }
}

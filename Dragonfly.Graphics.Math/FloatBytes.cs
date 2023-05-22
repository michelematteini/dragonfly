using System.Runtime.InteropServices;

namespace Dragonfly.Graphics.Math
{
    /// <summary>
    /// Allow to access the byte representation of a float.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct FloatBytes
    {
        [FieldOffset(0)]
        public uint Bytes;

        [FieldOffset(0)]
        public float Value;
    }

}

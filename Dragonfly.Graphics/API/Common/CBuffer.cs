using System;

namespace Dragonfly.Graphics.API
{
    internal class CBuffer
    {
        private byte[] buffer;

        public CBufferBinding Bindings { get; private set; }

        public CBuffer(CBufferBinding bindings)
        {
            Bindings = bindings;
            buffer = new byte[bindings.ByteSize];
            Changed = true;
        }

        public void SetValue(string name, int[] value)
        {
            Buffer.BlockCopy(value, 0, buffer, Bindings.GetByteAddress(name), value.Length * 4);
            Changed = true;
        }

        public void SetValue(string name, float[] value)
        {
            Buffer.BlockCopy(value, 0, buffer, Bindings.GetByteAddress(name), value.Length * 4);
            Changed = true;
        }

        public void SetValue(int nameHash, int[] value)
        {
            Buffer.BlockCopy(value, 0, buffer, Bindings.GetByteAddress(nameHash), value.Length * 4);
            Changed = true;
        }

        public void SetValue(int nameHash, float[] value)
        {
            Buffer.BlockCopy(value, 0, buffer, Bindings.GetByteAddress(nameHash), value.Length * 4);
            Changed = true;
        }

        public bool TrySetValue(int nameHash, int[] value)
        {
            int byteAddress;
            if (Bindings.TryGetByteAddress(nameHash, out byteAddress))
            {
                Buffer.BlockCopy(value, 0, buffer, byteAddress, value.Length * 4);
                Changed = true;
                return true;
            }
            return false;
        }

        public bool TrySetValue(int nameHash, float[] value)
        {
            int byteAddress;
            if (Bindings.TryGetByteAddress(nameHash, out byteAddress))
            {
                Buffer.BlockCopy(value, 0, buffer, byteAddress, value.Length * 4);
                Changed = true;
                return true;
            }
            return false;
        }

        public void SetValue(string name, int[] value, int length)
        {
            Buffer.BlockCopy(value, 0, buffer, Bindings.GetByteAddress(name), length * 4);
            Changed = true;
        }

        public void SetValue(string name, float[] value, int length)
        {
            Buffer.BlockCopy(value, 0, buffer, Bindings.GetByteAddress(name), length * 4);
            Changed = true;
        }

        public byte[] ToByteArray()
        {
            return buffer;
        }

        public bool Changed { get; set; }

        public void CopyTo(CBuffer other)
        {
#if DEBUG  
            if (other.Bindings.ByteSize != Bindings.ByteSize)
                throw new InvalidOperationException("Source and destination CBuffers are not compatible!");
#endif
            Buffer.BlockCopy(buffer, 0, other.buffer, 0, buffer.Length);
            other.Changed = true;
        }

        public CBuffer Clone()
        {
            CBuffer clone = new CBuffer(Bindings);
            CopyTo(clone);
            return clone;
        }

    }
}

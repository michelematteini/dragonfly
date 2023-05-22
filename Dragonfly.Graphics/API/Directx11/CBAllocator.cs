using Dragonfly.Graphics.API.Common;
using DragonflyGraphicsWrappers.DX11;
using System;
using System.Collections.Generic;


namespace Dragonfly.Graphics.API.Directx11
{
    internal class CBAllocator
    {
        private class CBReference
        {
            public DF_Buffer11 CBuffer;
            public int RefCount;

            public CBReference(DF_Buffer11 cBuffer)
            {
                CBuffer = cBuffer;
                RefCount = 1;

            }
        }

        private Dictionary<int, CBReference> buffers; // <size> -> <buffer> for shader buffer, -<hash> -> <buffer> for dedicated ones
        private DF_D3D11Device device;

        public CBAllocator(DF_D3D11Device device)
        {
            buffers = new Dictionary<int, CBReference>();
            this.device = device;
        }

        public DF_Buffer11 CreateCB(int byteSize, bool dedicatedResourceRequired)
        {
            CBReference cb = null;

            if (!dedicatedResourceRequired)
            {
                // search if one of the same size is already available
                if (buffers.TryGetValue(byteSize, out cb))
                    cb.RefCount++;
            }

            if(cb == null)
            {
                // create new cb
                DF_Buffer11 newCbResource = device.CreateConstantBuffer((uint)byteSize);
                cb = new CBReference(newCbResource);
                buffers.Add(dedicatedResourceRequired ? -newCbResource.GetResourceHash() : byteSize, cb);
            }

            return cb.CBuffer;
        }

        public void ReleaseCB(DF_Buffer11 buffer)
        {
            if (!ReleaseFromKey(buffer.GetByteSize()))
                ReleaseFromKey(-buffer.GetResourceHash());
        }

        private bool ReleaseFromKey(int key)
        {
            CBReference cbRef;
            if (!buffers.TryGetValue(key, out cbRef))
                return false;

            cbRef.RefCount--;
            if(cbRef.RefCount == 0)
            {
                cbRef.CBuffer.Release();
                buffers.Remove(key);
            }

            return true;
        }

    }



}

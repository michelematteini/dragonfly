using System;
using System.Collections.Generic;
using DragonflyGraphicsWrappers.DX12;

namespace Dragonfly.Graphics.API.Directx12
{
    /// <summary>
    /// Represents a single CBuffer that is frequently updated.
    /// CPU-side data can be changed accessing the Current version and then uploaded to the GPU using CommitVesionTo().
    /// </summary>
    internal class VersionedCBuffer
    {
        private DF_D3D12Device device;
        private List<DF_Resource12> versions; // each resource is a buffer which fits a cbuffer for each frame in the swap chain
        int cbByteSize, curVersion, frameIndex;

        public VersionedCBuffer(CBufferBinding bindings, DF_D3D12Device device)
        {
            this.device = device;
            Current = new CBuffer(bindings);
            versions = new List<DF_Resource12>();
            cbByteSize = DF_Directx3D12.PadCBufferSize(Current.Bindings.ByteSize);
            AddVersion();
        }

        public void NewFrame()
        {
            frameIndex = device.GetBackBufferIndex();
            curVersion = 0;
            Current.Changed = true;
        }

        public void CommitVersionTo(DF_CommandList12 cmdList)
        {
            // update resource data
            byte[] curData = Current.ToByteArray();
            versions[curVersion].SetData<byte>(curData, 0, frameIndex * cbByteSize, curData.Length, true);
            Current.Changed = false;

            // set to cmdList
            cmdList.SetGlobalConstantBuffer(versions[curVersion], frameIndex * cbByteSize);


            // add a new version if not available
            if (versions.Count == curVersion + 1)
                AddVersion();

            // move to the next version
            curVersion++;
        }

        private void AddVersion()
        {
            versions.Add(device.CreateBuffer(cbByteSize * DF_Directx3D12.GetBackbufferCount(), DF_CPUAccess.Write));
        }

        public CBuffer Current { get; private set; }

        public void Release()
        {
            foreach (DF_Resource12 cbufferVersion in versions)
                cbufferVersion.Release();
        }

    }
}

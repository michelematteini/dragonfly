using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using DragonflyGraphicsWrappers.DX12;
using System.Runtime.InteropServices;

namespace Dragonfly.Graphics.API.Directx12
{
    internal class InstancesVBuffer
    {
        private const int MIN_INSTANCES_PER_FRAME = 512;

        private DF_D3D12Device device;
        DF_CommandList12 cmdList;
        private DF_Resource12 uploadBuffer, vertexBuffer;
        private FrameDeferredReleaseList releaseList;
        private int maxInstancesPerFrame; // maximum number of total instances allocated for a single frame
        private int instanceByteSize; // the size in bytes of a single instance
        private int frameID; // the current frame ID
        private int nextFreeID; // next free instance index in the current buffer

        public InstancesVBuffer(DF_D3D12Device device, DF_CommandList12 cmdList, FrameDeferredReleaseList releaseList)
        {
            this.device = device;
            this.cmdList = cmdList;
            this.releaseList = releaseList;
            instanceByteSize = Marshal.SizeOf<Float4x4>();
            maxInstancesPerFrame = 0; // initially no buffer is available! it's created on-demand.
        }

        public void NewFrame()
        {
            frameID = device.GetBackBufferIndex();
            nextFreeID = 0;

            // queue a copy that will update the vb with the upload heap
            if (uploadBuffer != null)
                UpdateVertexBuffer();
        }

        public void SetInstances(DF_CommandList12 cmdList, ArrayRange<Float4x4> instances)
        {
            if(maxInstancesPerFrame - nextFreeID < instances.Count)
            {
                // buffer il full or cannot fit the specified instances!
                IncreaseCapacity(instances.Count);
            }

            int instancesByteOffset = (nextFreeID + frameID * maxInstancesPerFrame) * instanceByteSize;
            uploadBuffer.SetData<Float4x4>(instances.Buffer, instances.StartIndex, instancesByteOffset, instances.Count, true); // copy instances to the upload buffer
            cmdList.SetInstanceBuffer(vertexBuffer, (uint)instancesByteOffset, (uint)instanceByteSize, (uint)(instances.Count * instanceByteSize)); // bind instances
            nextFreeID += instances.Count;
        }

        private void UpdateVertexBuffer()
        {
            ulong instancesByteOffset = (ulong)(frameID * maxInstancesPerFrame * instanceByteSize);
            ulong instancesByteSize = (ulong)(maxInstancesPerFrame * instanceByteSize);
            cmdList.CopyBufferRegion(vertexBuffer, instancesByteOffset, uploadBuffer, instancesByteOffset, instancesByteSize);
        }

        private void IncreaseCapacity(int minCapacity)
        {
            // calc a new buffer size
            maxInstancesPerFrame = new Int3(MIN_INSTANCES_PER_FRAME, 2 * minCapacity, 2 * maxInstancesPerFrame).CMax();

            // release current buffers
            if (uploadBuffer != null)
            {
                releaseList.DeferredRelease(uploadBuffer);
                releaseList.DeferredRelease(vertexBuffer);
            }

            // create new buffers
            uploadBuffer = device.CreateVertexBuffer(instanceByteSize, maxInstancesPerFrame * DF_Directx3D12.GetBackbufferCount(), DF_CPUAccess.Write);
            vertexBuffer = device.CreateVertexBuffer(instanceByteSize, maxInstancesPerFrame * DF_Directx3D12.GetBackbufferCount(), DF_CPUAccess.None);

            // queue a copy the needed part of the new upload heap to the vb
            nextFreeID = 0;
            UpdateVertexBuffer();
        }

    }
}

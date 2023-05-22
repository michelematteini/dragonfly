using System;
using System.Collections.Generic;
using Dragonfly.Utils;
using DragonflyGraphicsWrappers.DX12;
using System.Threading;

namespace Dragonfly.Graphics.API.Directx12
{
    /// <summary>
    /// A class that manages memory allocation and upload of a list of cbuffer instances.
    /// An initial capacity in cbuffer slots can be specified, after which the size dynamically changes based on allocation requrests.
    /// </summary>
    internal class CBufferCollection
    {
        private class MemoryBlock
        {
            public DF_Resource12 UploadBuffer;
            public DF_Resource12 ShaderBuffer;
            public SlottedMemoryManager MemorySlots;
            public SlottedMemoryManager.Range ChangedRange; // the range of this buffer that has been modified in this frame
        }

        public class Item : SlottedMemoryManager.IResource
        {
            public int SlotSize;
            public SlottedMemoryManager.Range SlotRange;
            public int BlockID;

            public int GetSlotSize()
            {
                return SlotSize;
            }

            public void OnAllocation(SlottedMemoryManager.Range slots)
            {
                SlotRange = slots;
            }

            public void OnFree() { }
        }

        private List<MemoryBlock> blocks;
        private Directx12Graphics g;
        private int slotByteSize;
        private List<List<Item>> toBeReleased; // released cbuffer slots that will be actually reused only after the frame using it have been processed
        private object memSlotsLock;
        private ObjectPool<Item> allocPool;

        public CBufferCollection(Directx12Graphics g, int initialCapacity)
        {
            this.g = g;
            slotByteSize = DF_Directx3D12.PadCBufferSize(1);
            toBeReleased = new List<List<Item>>();
            for (int i = 0; i < DF_Directx3D12.GetBackbufferCount(); i++)
                toBeReleased.Add(new List<Item>());
            memSlotsLock = new object();
            blocks = new List<MemoryBlock>();
            allocPool = new ObjectPool<Item>(() => new Item(), item => item.SlotSize = 0);
            AddMemoryBlock(initialCapacity);
        }

        private void AddMemoryBlock(int slotCount)
        {
            MemoryBlock block = new MemoryBlock();
            block.UploadBuffer = g.Device.CreateBuffer(slotByteSize * slotCount, DF_CPUAccess.Write);
            block.ShaderBuffer = g.Device.CreateBuffer(slotByteSize * slotCount, DF_CPUAccess.None);
            block.MemorySlots = new SlottedMemoryManager(slotCount);
            block.ChangedRange = new SlottedMemoryManager.Range() { Start = 0, End = slotCount };
            blocks.Add(block);
        }

        public int ByteSize
        {
            get
            {
                int totalSlotCount = 0;
                for (int i = 0; i < blocks.Count; i++)
                {
                    totalSlotCount += blocks[i].MemorySlots.SlotCount;
                }
                return slotByteSize * totalSlotCount;
            }
        }


        public void OnNewFrame()
        {
            // release unused cbuffer slots
            foreach (Item releasedCB in toBeReleased[g.Device.GetBackBufferIndex()])
            {
                blocks[releasedCB.BlockID].MemorySlots.Free(releasedCB);
                allocPool.Free(releasedCB);
            }
            toBeReleased[g.Device.GetBackBufferIndex()].Clear();
        }

        private ulong ToMem(int slot)
        {
            return (ulong)(slotByteSize * slot);
        }

        public Item AddNewCBuffer(byte[] cbufferData)
        {
            Item allocation;

            lock(memSlotsLock)
            {
                // prepare allocation slots
                allocation = allocPool.CreateNew();
                allocation.SlotSize = DF_Directx3D12.PadCBufferSize(cbufferData.Length) / DF_Directx3D12.PadCBufferSize(1);
                if (!TryAlloc(allocation))
                {
                    // double the capacity and try again
                    AddMemoryBlock(ByteSize / slotByteSize);
                    if (!TryAlloc(allocation))
                    {
                        // allocation failure
#if DEBUG
                        throw new Exception("Failed to allocate a local cbuffer slot!");
#else
                        return null;
#endif
                    }
                }

                MemoryBlock mb = blocks[allocation.BlockID];

                // copy new data to the upload heap
                mb.UploadBuffer.SetData<byte>(cbufferData, 0, allocation.SlotRange.Start * slotByteSize, cbufferData.Length, true);

                // save changed range
                mb.ChangedRange.Start = System.Math.Min(mb.ChangedRange.Start, allocation.SlotRange.Start);
                mb.ChangedRange.End = System.Math.Max(mb.ChangedRange.End, allocation.SlotRange.End);
            }

            return allocation;
        }

        private bool TryAlloc(Item allocInfo)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].MemorySlots.TryAlloc(allocInfo))
                {
                    allocInfo.BlockID = i;
                    return true;
                }
            }

            return false;
        }

        public int GetByteOffset(Item cbuffer)
        {
            return cbuffer.SlotRange.Start * slotByteSize;
        }

        public DF_Resource12 GetParentResource(Item cbuffer)
        {
            return blocks[cbuffer.BlockID].ShaderBuffer;
        }

        public void ReleaseCBuffer(Item cbuffer)
        {
            if (cbuffer == null)
                return;

            lock (memSlotsLock)
            {
                toBeReleased[g.Device.GetBackBufferIndex()].Add(cbuffer);
            }
        }

        public void UpdateCBuffers()
        {
            // copy the upload resource to the one used in shaders
            // TODO: the upload resource could be created much smaller, it just have to fit the region to be updated!
            // TODO: frequently updated shader cbuffers could be placed on a different resource
            foreach (MemoryBlock mb in blocks)
            {
                if (mb.ChangedRange.Size > 0)
                {
                    g.InnerCommandList.CopyBufferRegion(mb.ShaderBuffer, ToMem(mb.ChangedRange.Start), mb.UploadBuffer, ToMem(mb.ChangedRange.Start), ToMem(mb.ChangedRange.Size));
                }

                // reset changed range
                mb.ChangedRange = new SlottedMemoryManager.Range() { Start = mb.MemorySlots.SlotCount, End = 0 }; // invalid and empty range
            }
        }

        public void Release()
        {
            foreach (MemoryBlock mb in blocks)
            {
                mb.UploadBuffer.Release();
                mb.ShaderBuffer.Release();
            }
        }

    }
}

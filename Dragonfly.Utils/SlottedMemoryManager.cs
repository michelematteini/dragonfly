using System;
using System.Collections.Generic;

namespace Dragonfly.Utils
{
    
   
    /// <summary>
    /// Memory allocation helper that manage allocations on a slotted memory space of a given size.
    /// </summary>
    public class SlottedMemoryManager
    {
        public struct Range
        {
            public int Start, End;
            public int Size => End - Start;
        }
        public interface IResource
        {
            int GetSlotSize();

            void OnAllocation(SlottedMemoryManager.Range slots);

            void OnFree();
        }

        private List<Range> freeSlots;
        private Dictionary<IResource, Range> allocations;

        public SlottedMemoryManager(int slotCount)
        {
            freeSlots = new List<Range>();
            freeSlots.Add(new Range() { Start = 0, End = slotCount });
            allocations = new Dictionary<IResource, Range>();
            SlotCount = slotCount;
            UsedSlots = 0;
        }

        public int SlotCount { get; private set; }

        public int UsedSlots { get; private set; }

        public bool TryAlloc(IResource resource)
        {
            if (allocations.ContainsKey(resource))
                throw new Exception("Momory slots for this resource have already been allocated!");

            Range slotRange = AllocateRange(resource.GetSlotSize());
            if (slotRange.Size == 0)
                return false; // not enough slots (or memory too fragmented)!
            resource.OnAllocation(slotRange);
            allocations[resource] = slotRange;
            return true;
        }

        public void Free(IResource resource)
        {
            Range slotRange;
            if(allocations.TryGetValue(resource, out slotRange))
            {
                allocations.Remove(resource);
                UsedSlots -= slotRange.Size;
                resource.OnFree();
                freeSlots.Add(slotRange);
            }
        }

        private Range AllocateRange(int slotSize)
        {
            // find a free range to be used
            for (int i = freeSlots.Count - 1; i >= 0; i--)
            {
                Range r = freeSlots[i];
                if (r.Size < slotSize)
                    continue; // skip: range does not fit the required size

                if (r.Size > slotSize)
                {
                    // use a part of the oversized free range
                    r.End = r.Start + slotSize;
                    freeSlots[i] = new Range() { Start = r.End, End = freeSlots[i].End };
                }
                else
                {
                    // exact range found, just remove it from the free slots
                    freeSlots.RemoveAt(i);
                }

                UsedSlots += slotSize;
                return r;
            }

            return new Range();
        }

    }
}

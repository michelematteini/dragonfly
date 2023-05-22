using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Base class for the management of allocations of on a 2d atlas space.
    /// </summary>
    public abstract class AtlasLayout
    {
        public AtlasLayout()
        {
        }

        public abstract Int2 Resolution { get; }

        public abstract bool TryAllocateSubTexture(Int2 preferredSize, out SubTextureReference subTexture);

        public List<SubTextureReference> TryAllocateSubTextureArray(Int2 preferredSize, int arrayLength)
        {
            List<SubTextureReference> allocatedTextures = new List<SubTextureReference>();
            for (int i = 0; i < arrayLength; i++)
            {
                SubTextureReference newRef;
                if (!TryAllocateSubTexture(preferredSize, out newRef))
                    break;
                allocatedTextures.Add(newRef);
            }

            if (allocatedTextures.Count < arrayLength)
            {
                // failed to allocate all of them, release them and fail
                foreach (SubTextureReference newRef in allocatedTextures)
                    newRef.Release();

                allocatedTextures.Clear();
            }

            return allocatedTextures;
        }

        public abstract void ReleaseSubTexture(SubTextureReference subTexture);
        
    }

    public class SubTextureReference
    {
        public AtlasLayout ParentLayout { get; private set; }

        public AARect Area { get; protected set; }

        public SubTextureReference(AARect area, AtlasLayout parent)
        {
            Area = area;
            ParentLayout = parent;
        }

        public Int2 Resolution
        {
            get { return (Int2)(Area.Size * ParentLayout.Resolution); }
        }

        public void Release()
        {
            ParentLayout.ReleaseSubTexture(this);
        }

    }
}

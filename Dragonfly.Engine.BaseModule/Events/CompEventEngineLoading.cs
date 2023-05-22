using System;
using System.Collections.Generic;
using Dragonfly.Engine.Core;

namespace Dragonfly.BaseModule
{
    public class CompEventEngineLoading : Component
    {
        public CompEventEngineLoading(Component owner) : this(owner, EventTriggerType.Occurring) { }

        public CompEventEngineLoading(Component owner, EventTriggerType trigger) : base(owner) 
        {
            Event = new CompEvent(this, IsOccurring, trigger);
        }

        public CompEvent Event { get; private set; }

        private bool IsOccurring()
        {
            // check if any texture loading is pending
            if (GetComponent<CompTextureLoader>().IsLoading)
                return true;

            // check if any obj model is loading            
            if (GetComponent<CompObjToMesh>().IsLoading)
                return true;

            // check if any graphics resource loading is required
            IReadOnlyList<ICompAllocator> allocators = GetComponents<ICompAllocator>();
            for (int i = 0; i < allocators.Count; i++)
            {
                if (allocators[i].LoadingRequired)
                    return true;
            }

            return false;
        }
    }
}

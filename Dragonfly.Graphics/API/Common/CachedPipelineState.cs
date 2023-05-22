using Dragonfly.Utils;
using DragonflyGraphicsWrappers.DX11;
using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Common
{
    internal abstract class CachedPipelineState<TDesc, TOut, TDevice> where TDesc : ObservableRecord
    {
        private Dictionary<int, TOut> cachedStates;

        protected TDevice Device { get; private set; }

        public CachedPipelineState(TDevice device)
        {
            cachedStates = new Dictionary<int, TOut>();
            Device = device;
        }

        public void CacheState(TDesc stateDesc)
        {
            int stateHash = stateDesc.GetHashCode();
            if (!cachedStates.ContainsKey(stateHash))
            {
                TOut newState = CreateState(stateDesc);
                cachedStates[stateHash] = newState;
            }
        }

        public TOut GetState(TDesc stateDesc)
        {
            int stateHash = stateDesc.GetHashCode();
            TOut state;
            if (!cachedStates.TryGetValue(stateHash, out state))
                throw new Exception("The requested object is not available! CacheState() was never called with this description.)");
            return state;
        }

        public void CacheAllStates()
        {
            foreach(TDesc stateDesc in GenerateAllStateDescriptions())
                CacheState(stateDesc);
        }

        protected abstract TOut CreateState(TDesc stateDesc);

        protected abstract void ReleaseState(TOut state);

        protected abstract IEnumerable<TDesc> GenerateAllStateDescriptions();

        public void Release()
        {
            foreach (TOut cachedState in cachedStates.Values)
                ReleaseState(cachedState);

            cachedStates.Clear();
        }

    }
}

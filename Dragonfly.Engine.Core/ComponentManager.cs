using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Dragonfly.Engine.Core
{
    internal class ComponentManager
    {
        private class MatQueryCacheEntry
        {
            public SortedLinkedList<CompMaterial> Result;
            public List<MaterialClassFilter> Filters;
            /// <summary>
            /// UpdateID of the last frame in which this cache entry was used.
            /// </summary>
            public int LastUsed;

            public MatQueryCacheEntry(List<MaterialClassFilter> query)
            {
                Result = new SortedLinkedList<CompMaterial>(Comparer<CompMaterial>.Create((m1, m2) => m1.RenderOrder.CompareTo(m2.RenderOrder)));
                Filters = query;
            }
        }

        private class UpdatableQueryEntry
        {
            public UpdateType Type;
            public Queue<ICompUpdatable> ToBeUpdated;
        }

        private Dictionary<int, Component> components; // all components, indexed by ID
        private Dictionary<Type, IInvariantSet> byTypeCache; // all components, grouped by Type, only contains Types that have been recently searched for
        private object byTypeCacheLock; // lock for byTypeCache
        private object allocationLock; // lock of ICompAllocator.LoadResources()
        private Dictionary<int, MatQueryCacheEntry> matQueryCache; // material-specific cache: query ID -> query cache record
        private UpdatableQueryEntry[] updateQueryCache; // updatable-specific cache: update type -> list of updatable that requested that update on previous frame
        private int lastUpdateCacheID; // last update ID in which the updateQueryCache was updated
        private Dictionary<int, Component> waitingDisposal; // all component that should be disposed of next frame
        private List<ICompAllocator> inactiveAllocators; // inactive ICompAllocator (that should still be polled for resource allocation

        public ComponentManager()
        {
            components = new Dictionary<int, Component>();
            byTypeCache = new Dictionary<Type, IInvariantSet>();
            byTypeCacheLock = new object();
            matQueryCache = new Dictionary<int, MatQueryCacheEntry>();
            updateQueryCache = new UpdatableQueryEntry[3];
            updateQueryCache[0] = new UpdatableQueryEntry() { Type = UpdateType.FrameStart1, ToBeUpdated = new Queue<ICompUpdatable>() };
            updateQueryCache[1] = new UpdatableQueryEntry() { Type = UpdateType.FrameStart2, ToBeUpdated = new Queue<ICompUpdatable>() };
            updateQueryCache[2] = new UpdatableQueryEntry() { Type = UpdateType.ResourceLoaded, ToBeUpdated = new Queue<ICompUpdatable>() };
            UpdateID = 1;
            allocationLock = new object();
            lastUpdateCacheID = 0;
            waitingDisposal = new Dictionary<int, Component>();
            inactiveAllocators = new List<ICompAllocator>();
        }

        public void Add(Component c)
        {
#if VERBOSE
            if (c.Context != null && c.Context.Scene != null)
                c.Context.Scene.Log.WriteLine("Adding new component: {0}", c);
#endif
            this.components.Add(c.ID, c);
     
            //update by type query cache
            foreach (Type queryType in byTypeCache.Keys)
                byTypeCache[queryType].AddIfTypeIsCompatible(c);

            // update drawable cache
            if (c is CompMaterial m)
                AddMaterial(m);
        }

        public void QueueDisposal(Component component)
        {
            waitingDisposal[component.ID] = component;
        }

        public void Remove(Component c)
        {
            this.components.Remove(c.ID);

            // search and remove from type cache
            foreach (Type queryType in byTypeCache.Keys)
                byTypeCache[queryType].Remove(c);

            // update drawable cache
            if (c is CompMaterial m)
                RemoveMaterial(m);
        }

        public IReadOnlyList<T> Query<T>() where T : IComponent
        {
            Type queryType = typeof(T);
            IInvariantSet queryResult;

            if (!byTypeCache.TryGetValue(queryType, out queryResult))
            {
                //query for type T
                queryResult = new InvariantSet<T>();
                foreach (object obj in components.Values)
                    queryResult.AddIfTypeIsCompatible(obj);

                lock (byTypeCacheLock)
                {
                    byTypeCache[queryType] = queryResult;
                }
            }

            return (queryResult as InvariantSet<T>).Values;
        }

        public int GetCount<T>() where T: IComponent
        {
            return Query<T>().Count;
        }

        public T QueryFirst<T>() where T : IComponent
        {
            IReadOnlyList<T> queryResult = Query<T>();
            if (queryResult.Count == 0) return default(T);
            return queryResult[0];
        }

        public void Clear()
        {
            PerformWaitingDisposals();
            components.Clear();
            byTypeCache.Clear();
        }

        public int Count
        {
            get
            {
                return components.Count;
            }
        }

        public void OnNewFrameStart()
        {
            unchecked { UpdateID = UpdateID + 1; }
            PerformWaitingDisposals();
            CleanMaterialCache();
        }

        public void PerformWaitingDisposals()
        {
            foreach (Component c in waitingDisposal.Values)
                c.OnDispose();
            waitingDisposal.Clear();
        }

        const int MAT_CACHE_LIFESPAN = 100;
        List<int> matCacheToBeRemoved = new List<int>();
        private void CleanMaterialCache()
        {
            int lastPreservedID = UpdateID - MAT_CACHE_LIFESPAN;

            // search cache entries that are too old and can be removed
            matCacheToBeRemoved.Clear();
            foreach (KeyValuePair<int, MatQueryCacheEntry> cacheEntry in matQueryCache)
            {
                if (cacheEntry.Value.LastUsed < lastPreservedID)
                    matCacheToBeRemoved.Add(cacheEntry.Key);
            }

            // remove them from cache
            foreach (int toBeRemovedID in matCacheToBeRemoved)
                matQueryCache.Remove(toBeRemovedID);
        }
        
        public int UpdateID
        {
            get; 
            private set;
        }

        #region Material queries

        private void AddMaterial(CompMaterial m)
        {
            if (m.Class == null)
                return; // from material constructor, still no configurations.

            foreach (int queryId in matQueryCache.Keys)
            {
                if (MaterialClassFilter.ApplyList(matQueryCache[queryId].Filters, m))
                    matQueryCache[queryId].Result.Add(m);
            }
        }

        private bool RemoveMaterial(CompMaterial m)
        {
            bool removed = false;

            // remove materials from each cached result
            foreach (MatQueryCacheEntry passDrawList in matQueryCache.Values)
                removed |= passDrawList.Result.Remove(m);

            return removed;
        }

        public void UpdateMaterialQueries(CompMaterial m)
        {
            if (m.Active)
            {
                // refresh by adding and removing
                RemoveMaterial(m);
                AddMaterial(m);
            }
        }

        public SortedLinkedList<CompMaterial> QueryMaterials(List<MaterialClassFilter> filterList)
        {
            int queryId = MaterialClassFilter.GetQueryHash(filterList);
            MatQueryCacheEntry materialQuery;
            if (!matQueryCache.TryGetValue(queryId, out materialQuery))
            { // cache not available

                // perform the full query
                materialQuery = new MatQueryCacheEntry(new List<MaterialClassFilter>(filterList));
                foreach(CompMaterial m in Query<CompMaterial>())
                {
                    if (MaterialClassFilter.ApplyList(filterList, m))
                        materialQuery.Result.Add(m);
                }

                // cache the result
                matQueryCache[queryId] = materialQuery;
            }

            materialQuery.LastUsed = UpdateID;
            return materialQuery.Result;
        }

        #endregion

        #region Updatables

        QueryUpdatablesForBody queryUpdatesForBody = new QueryUpdatablesForBody();
        private void FillUpdateCacheIfNeeded()
        {
            if (lastUpdateCacheID == UpdateID)
                return;

            // multithreaded search for components to be updated
            queryUpdatesForBody.Updatables = Query<ICompUpdatable>();
            queryUpdatesForBody.UpdateQueryCache = updateQueryCache;
            SlimParallel.For(0, queryUpdatesForBody.Updatables.Count, 10, queryUpdatesForBody);

            lastUpdateCacheID = UpdateID;
        }

        private class QueryUpdatablesForBody : SlimParallel.IForBody
        {
            public IReadOnlyList<ICompUpdatable> Updatables;
            public UpdatableQueryEntry[] UpdateQueryCache;

            public void Execute(int i)
            {
                ICompUpdatable u = Updatables[i];

                UpdateType neededUpdates = u.NeededUpdates;

                for (int uTypeID = 0; uTypeID < UpdateQueryCache.Length; uTypeID++)
                {
                    UpdatableQueryEntry query = UpdateQueryCache[uTypeID];
                    if ((neededUpdates & query.Type) == query.Type)
                    {
                        lock (query.ToBeUpdated)
                        {
                            query.ToBeUpdated.Enqueue(u);
                        }
                    }
                }
            }
        }

        private UpdatableQueryEntry GetUpdatablesQueryEntry(UpdateType updateType)
        {
            for (int uTypeID = 0; uTypeID < updateQueryCache.Length; uTypeID++)
            {
                if (updateQueryCache[uTypeID].Type == updateType)
                    return updateQueryCache[uTypeID];
            }

            return null;
        }

        public void UpdateComponents(IDFGraphics g, UpdateType updateType)
        {
            FillUpdateCacheIfNeeded();

            UpdatableQueryEntry updateCache = GetUpdatablesQueryEntry(updateType);

            while (updateCache.ToBeUpdated.Count > 0)
            {
                ICompUpdatable u = updateCache.ToBeUpdated.Dequeue();
#if TRACING
                g.StartTracedSection(Color.TransparentWhite, u.GetType().Name);
#endif
                u.Update(updateType);
#if TRACING
                g.EndTracedSection();
#endif
            }            
        }

        #endregion

        #region Allocators

        LoadResourcesArgs loadResForBody = new LoadResourcesArgs();
        public void LoadComponentResources(IDFGraphics g, EngineResourceAllocator resAllocator)
        {
            // trigger all resource allocator components that require to be invoked
            IReadOnlyList<ICompAllocator> allocatorComponents = Query<ICompAllocator>();
            loadResForBody.AllocatorComponents = allocatorComponents;
            loadResForBody.Graphics = g;
            loadResForBody.ResAllocator = resAllocator;
            loadResForBody.AllocationLock = allocationLock;
            SlimParallel.For(0, allocatorComponents.Count, 10, loadResForBody);
            loadResForBody.AllocationLock = inactiveAllocators;
            SlimParallel.For(0, allocatorComponents.Count, 10, loadResForBody);
        }

        private class LoadResourcesArgs : SlimParallel.IForBody
        {
            internal IReadOnlyList<ICompAllocator> AllocatorComponents;
            internal IDFGraphics Graphics;
            internal EngineResourceAllocator ResAllocator;
            internal object AllocationLock;

            public void Execute(int i)
            {
                ICompAllocator allocator = AllocatorComponents[i];

                if (!allocator.LoadingRequired)
                    return;
#if VERBOSE
                Log.WriteLine("Loading Reosurces, component: " + allocator);
#endif
                lock (AllocationLock)
                {
#if TRACING
                    Graphics.StartTracedSection(Color.TransparentWhite, allocator.GetType().Name);
#endif
                    allocator.LoadGraphicResources(ResAllocator);
#if TRACING
                    Graphics.EndTracedSection();
#endif
                }
            }
        }

        public void ReleaseComponentResources()
        {
            IReadOnlyList<ICompAllocator> resAllocators = Query<ICompAllocator>();
            for (int i = 0; i < resAllocators.Count; i++)
                resAllocators[i].ReleaseGraphicResources();
        }

        public void SetActive(Component c, bool active)
        {
            ICompAllocator ca = c as ICompAllocator;
            if (active)
            {
                if (ca != null)
                    inactiveAllocators.Remove(ca);
                Add(c);
            }
            else
            {
                if (ca != null)
                    inactiveAllocators.Add(ca);
                Remove(c);
            }
        }

        #endregion

    }

}

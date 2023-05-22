using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dragonfly.BaseModule
{
    public class BakerScreenSpacePool
    {
        private Dictionary<int, Queue<CompBakerScreenSpace>> bakersCache;

        public BakerScreenSpacePool()
        {
            bakersCache = new Dictionary<int, Queue<CompBakerScreenSpace>>();
        }

        private Queue<CompBakerScreenSpace> GetBakerCacheFor(int bakerHash)
        {
            Queue<CompBakerScreenSpace> cacheQueue;
            if (!bakersCache.TryGetValue(bakerHash, out cacheQueue))
            {
                // first baker of this type, initialize cache
                cacheQueue = new Queue<CompBakerScreenSpace>();
                bakersCache[bakerHash] = cacheQueue;
            }
            return cacheQueue;
        }

        public void ReleaseBaker(CompBakerScreenSpace baker)
        {
            baker.Material = null;
            Queue<CompBakerScreenSpace> cacheQueue = GetBakerCacheFor(GetBakerHash(baker));
            cacheQueue.Enqueue(baker);
        }

        private int GetBakerHash(Int2 resolution, SurfaceFormat[] formats)
        {
            HashCode hash = new HashCode();
            hash.Reset();
            hash.Add(resolution.X, 16);
            hash.Add(resolution.Y, 16);
            for (int i = 0; i < formats.Length; i++)
                hash.Add(formats[i], 4);
            return hash.Resolve();
        }

        private int GetBakerHash(CompBakerScreenSpace baker)
        {
            SurfaceFormat[] formats = new SurfaceFormat[baker.Baker.FinalPass.RenderBuffer.SurfaceCount];
            for (int i = 0; i < formats.Length; i++)
                formats[i] = baker.Baker.FinalPass.RenderBuffer.GetSurfaceFormat(i);
            return GetBakerHash(baker.Baker.FinalPass.Resolution, formats);
        }

        public CompBakerScreenSpace CreateBaker(Component parent, string stepName, Int2 resolution, SurfaceFormat[] formats)
        {
            int bakerHash = GetBakerHash(resolution, formats);
            Queue<CompBakerScreenSpace> cacheQueue = GetBakerCacheFor(bakerHash);

            // create baker if not available
            CompBakerScreenSpace baker;
            if (cacheQueue.Count == 0)
            {
                baker = new CompBakerScreenSpace(parent, resolution, formats);
                baker.Baker.DisposeOnCompletion = false;
                baker.Baker.BakeOnlyOnce = false;
                baker.Baker.BakeStartEventIsTrigger = false;
            }
            else
            {
                baker = cacheQueue.Dequeue();
            }

            baker.Baker.FinalPass.Name = stepName;
            return baker;
        }


    }
}

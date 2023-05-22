using System;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Dragonfly.BaseModule
{
    internal class CompTextureLoader : Component, ICompAllocator, ILoadedFileHandler
    {
        private const int PRESERVE_TEXTURE_FRAME_COUNT = 5; // wait in frames before releasing an unused texture, to allow for components to update 

        private AsyncFileLoader fileLoader; // used to load texture files
        private object LOADING_QUEUE_LOCK; // lock to be taken before accessing any other loading queues

        // loading queues
        private BlockingQueue<CompTextureRef> needFileQueue;
        private BlockingQueue<CompTextureRef> waitingAllocationQueue;
        private List<CompTextureRef> priorityAllocationQueue;
        private List<CompTextureRef> placeholderQueue;
        private List<CompTextureRef> waitingFileQueue;
        private Queue<UnusedTexture> toBeReleased;

        // texture source cache
        private Dictionary<Byte4, Texture> colorTextures;
        private Dictionary<string, TextureFile> diskTextureCache; // ref-counted textures loaded from disk, indexed by file path
        private Dictionary<int, DynamicTexture> dynamicTextures; // ref-counted texture created at runtime


        public CompTextureLoader(Component owner) : base(owner)
        {
            fileLoader = new AsyncFileLoader();
            LOADING_QUEUE_LOCK = new object();

            needFileQueue = new BlockingQueue<CompTextureRef>();
            waitingAllocationQueue = new BlockingQueue<CompTextureRef>();
            placeholderQueue = new List<CompTextureRef>();
            waitingFileQueue = new List<CompTextureRef>();
            priorityAllocationQueue = new List<CompTextureRef>();
            toBeReleased = new Queue<UnusedTexture>();

            colorTextures = new Dictionary<Byte4, Texture>();
            diskTextureCache = new Dictionary<string, TextureFile>();
            dynamicTextures = new Dictionary<int, DynamicTexture>();

            MegapixelsPerFrame = 2.0f;
        }

        /// <summary>
        /// The per-frame thoughput budget of this component. 
        /// <para/> Higher values means higher loading speed, at the cost of a more noticeable drop in framerate.
        /// </summary>
        public float MegapixelsPerFrame { get; set; }

        public bool LoadingRequired { get; internal set; }

        public bool IsLoading
        {
            get
            {
                return LoadingRequired || !fileLoader.Idle;
            }
        }

        /// <summary>
        /// Start loading a texture from the source of the specified texture reference.
        /// </summary>
        public void BeginLoading(CompTextureRef textureRef)
        {
            lock (LOADING_QUEUE_LOCK)
            {
                if (TryLoadFromCache(textureRef))
                    return; // no need to queue, resource already available.

                switch (textureRef.Source)
                {
                    case TexRefSource.File:
                        needFileQueue.Enqueue(textureRef);
                        break;
                    case TexRefSource.Bitmap:
                    case TexRefSource.Dynamic:
                        QueueForAllocation(textureRef);
                        break;
                }
            }
            LoadingRequired = true;
        }

        /// <summary>
        /// Start loading a placeholder texture for the specified texture reference.
        /// </summary>
        /// <param name="textureRef"></param>
        public void RequestPlaceholder(CompTextureRef textureRef)
        {
            lock (LOADING_QUEUE_LOCK)
            {
                placeholderQueue.Add(textureRef);
            }
            LoadingRequired = true;
        }

        /// <summary>
        /// Remove the specified reference from the current loading queue if present. 
        /// </summary>
        public void StopLoading(CompTextureRef textureRef, bool keepPlaceholder = true)
        {
            lock (LOADING_QUEUE_LOCK)
            {
                needFileQueue.Remove(textureRef);
                waitingAllocationQueue.Remove(textureRef);
                priorityAllocationQueue.Remove(textureRef);
                if (!keepPlaceholder) placeholderQueue.Remove(textureRef);
                waitingFileQueue.Remove(textureRef);
            }
        }

        public void LoadGraphicResources(EngineResourceAllocator g)
        {
            if (!fileLoader.IsRunning) fileLoader.Start();

            if (!Monitor.TryEnter(LOADING_QUEUE_LOCK))
                return;

            try
            {
                // process all file loading requests
                while (needFileQueue.Count > 0)
                {
                    CompTextureRef tex;
                    if (!needFileQueue.TryDequeue(out tex))
                        break; // file request queue is locket up, skip file requests

                    if (diskTextureCache.ContainsKey(tex.SrcPath))
                    {
                        TextureFile texFile = diskTextureCache[tex.SrcPath];
                        if (!texFile.Loaded)
                        {
                            // file is still loading, just add this reference to the waiting queue
                            waitingFileQueue.Add(tex);
                        }
                        else
                        {
                            // file is ready in cache, move to allocation queue
                            QueueForAllocation(tex);
                        }
                    }
                    else
                    {
                        // move to file waiting queue
                        waitingFileQueue.Add(tex);

                        // create a new file cache entry
                        TextureFile texFile = new TextureFile();
                        diskTextureCache[tex.SrcPath] = texFile;

                        // request loading from disk
                        if (!tex.LoadNow)
                            fileLoader.RequestFile(tex.ID, tex.SrcPath, this, true);
                        else
                            OnFileLoaded(tex.ID, tex.SrcPath, File.ReadAllBytes(tex.SrcPath));
                    }
                }

                // allocate textures
                {
                    // allocate normal priority textures up to the specified ammount in MPix/frame
                    float pixelBudget = MegapixelsPerFrame * 1048576.0f;
                    while (waitingAllocationQueue.Count > 0 && pixelBudget > 0)
                    {
                        CompTextureRef tex;
                        if (!waitingAllocationQueue.TryDequeue(out tex))
                            break;

                        TryCreateTexture(g, tex, ref pixelBudget);
                    }

                    // allocate all the high-priority textures immediately
                    foreach (CompTextureRef tex in priorityAllocationQueue)
                        TryCreateTexture(g, tex, ref pixelBudget);
                    priorityAllocationQueue.Clear();
                }

                // create requested placeholder textures
                {
                    foreach (CompTextureRef tex in placeholderQueue)
                    {
                        if (tex.Available) 
                            continue; // a value is already available (this could also be a previous texture), no placeholder needed.

                        tex.OnPlaceholderCreated(CreateTextureFromColor(tex.PlaceholderColor, g));
                    }
                    placeholderQueue.Clear();
                }

                // release unused resources
                {
                    while(toBeReleased.Count > 0 && (Context.Time.FrameIndex - toBeReleased.Peek().FromFrame) >= PRESERVE_TEXTURE_FRAME_COUNT)
                        toBeReleased.Dequeue().Resource.Release();
                }

                // keep loading until all loading queues are empty
                LoadingRequired = (needFileQueue.Count + waitingAllocationQueue.Count + priorityAllocationQueue.Count + placeholderQueue.Count + waitingFileQueue.Count + toBeReleased.Count) > 0;
            }
            finally
            {
                Monitor.Exit(LOADING_QUEUE_LOCK);
            }
        }

        private void QueueForAllocation(CompTextureRef texRef)
        {
            if (texRef.LoadNow)
                priorityAllocationQueue.Add(texRef);
            else
                waitingAllocationQueue.Enqueue(texRef);
        }

        public void OnFileLoaded(int requestID, string filePath, byte[] loadedBytes)
        {
            byte[] decodedPixBytes = null;
            int imgWidth = 0, imgHeight = 0;

            // decode file if needed
            switch (Path.GetExtension(filePath).DefaultIfNull("").ToLower())
            {
                case HdrFile.Extension:
                    {
                        HdrFile hdrImage = new HdrFile(filePath);
                        imgWidth = hdrImage.Header.Width;
                        imgHeight = hdrImage.Header.Height;
                        decodedPixBytes = new byte[hdrImage.PixelCount * 4];
                        hdrImage.CopyRGBEDataTo(decodedPixBytes);
                        break;
                    }

                default:
                    // no decoding: the texture will be created from loadedBytes directly
                    break;
            }

            lock (LOADING_QUEUE_LOCK)
            {
                // fill this texture file istance (added when the file was requested)
                TextureFile texFile = diskTextureCache[filePath];
                texFile.SrcBytes = loadedBytes;
                texFile.DecodedRGBA = decodedPixBytes;
                texFile.SrcWidth = imgWidth;
                texFile.SrcHeight = imgHeight;
                texFile.Loaded = true;

                // unlock all textures waiting for this file
                for (int i = waitingFileQueue.Count - 1; i >= 0; i--)
                {
                    if (waitingFileQueue[i].SrcPath != filePath)
                        continue; // not waiting this file

                    // move to the allocation queue
                    QueueForAllocation(waitingFileQueue[i]);
                    waitingFileQueue.RemoveAt(i);
                    LoadingRequired = true;
                }
            }
        }

        private bool TryCreateTexture(EngineResourceAllocator g, CompTextureRef destReference, ref float pixelBudgetLeft)
        {
            // check if its already available in cache
            if (TryLoadFromCache(destReference))
                return true;

            // not available, try creating a new texture resource
            Texture loadedTexture = null;
            switch (destReference.Source)
            {
                case TexRefSource.File:
                    {
                        TextureFile texFile = diskTextureCache[destReference.SrcPath];
                        if (texFile.ContainsDecodedData)
                        { // from data
                            texFile.Texture = g.CreateTexture(texFile.SrcWidth, texFile.SrcHeight, Graphics.SurfaceFormat.Color);
                            texFile.Texture.SetData<byte>(texFile.DecodedRGBA);
                        }
                        else
                        { // from file bytes

                            texFile.Texture = g.CreateTexture(texFile.SrcBytes);
                        }

                        texFile.ClearCache();
                        texFile.RefCount = 1;
                        loadedTexture = texFile.Texture;
                    }
                    break;

                case TexRefSource.Bitmap:
                    loadedTexture = g.CreateTexture(destReference.SrcBitmap);
                    break;

                case TexRefSource.Dynamic:
                    {
                        DynamicTexture dynTex = new DynamicTexture();
                        dynTex.Texture = loadedTexture = g.CreateTexture(destReference.SrcParams.Resolution.Width, destReference.SrcParams.Resolution.Height, destReference.SrcParams.Format);
                        if (destReference.SrcParams.TextureInitializer != null)
                            destReference.SrcParams.TextureInitializer(loadedTexture);
                        dynamicTextures[destReference.SrcParams.ID] = dynTex;
                        dynamicTextures[destReference.SrcParams.ID].RefCount = 1;
                    }
                    break;
            }

            if (loadedTexture == null)
                return false;

            pixelBudgetLeft -= (loadedTexture.Width * loadedTexture.Height);
            SetLoadedValue(destReference, loadedTexture);
            return true;
        }

        public bool TryLoadFromCache(CompTextureRef tex)
        {
            Texture cachedTexture = null;

            switch (tex.Source)
            {
                case TexRefSource.File:
                    {
                        TextureFile texFile;
                        if (diskTextureCache.TryGetValue(tex.SrcPath, out texFile) && texFile.Texture != null)
                        {
                            cachedTexture = texFile.Texture;
                            texFile.RefCount += 1;
                        }
                    }
                    break;

                case TexRefSource.Dynamic:
                    {
                        DynamicTexture dynTex;
                        if (dynamicTextures.TryGetValue(tex.SrcParams.ID, out dynTex))
                        {
                            cachedTexture = dynTex.Texture;
                            dynTex.RefCount += 1;
                        }
                    }
                    break;
            }

            if (cachedTexture == null)
                return false;

            SetLoadedValue(tex, cachedTexture);
            return true;
        }

        private void SetLoadedValue(CompTextureRef tex, Texture loadedTexture)
        {
            ReleaseLoadedValue(tex);
            tex.OnLoadingCompleted(loadedTexture);
        }

        /// <summary>
        /// Decrease the reference count to the loaded resource loaded by the specified texture reference.
        /// </summary>
        /// <param name="tex"></param>
        public void ReleaseLoadedValue(CompTextureRef tex)
        {
            lock (LOADING_QUEUE_LOCK)
            {
                if (tex.LoadedSource == TexRefSource.File)
                {
                    if (diskTextureCache.ContainsKey(tex.LoadedSrcPath))
                    {
                        TextureFile curRefList = diskTextureCache[tex.LoadedSrcPath];
                        curRefList.RefCount -= 1;
                        if (curRefList.RefCount == 0)
                        {
                            QueueRelease(curRefList.Texture);
                            diskTextureCache.Remove(tex.LoadedSrcPath);
                        }
                    }
                }
                else if (tex.LoadedSource == TexRefSource.Bitmap)
                {
                    QueueRelease(tex.TexValue);
                }
                else if (tex.LoadedSource == TexRefSource.Dynamic)
                {
                    DynamicTexture dynTex = dynamicTextures[tex.LoadedParams.ID];
                    dynTex.RefCount -= 1;
                    if (dynTex.RefCount == 0)
                    {
                        QueueRelease(dynTex.Texture);
                        dynamicTextures.Remove(tex.LoadedParams.ID);
                    }
                }
            }

            tex.OnLoadedValueReleased();
        }

        private void QueueRelease(Texture tex)
        {
            toBeReleased.Enqueue(new UnusedTexture() { Resource = tex, FromFrame = Context.Time.FrameIndex });
        }

        public void ReleaseGraphicResources()
        {
            // stop file loading
            fileLoader.Stop(true);

            // release textures and move them to the loading queue (so that when LoadResources() is called they're restored
            lock (LOADING_QUEUE_LOCK)
            {
                foreach (CompTextureRef tex in GetComponents<CompTextureRef>())
                {
                    if (!tex.IsPlaceholder)
                    {
                        StopLoading(tex);
                        ReleaseLoadedValue(tex);
                        BeginLoading(tex);
                    }

                    RequestPlaceholder(tex);
                }
            }

            // release placeholders
            foreach (Byte4 placeHolderColor in colorTextures.Keys)
                colorTextures[placeHolderColor].Release();
            colorTextures.Clear();

            LoadingRequired = true;
        }

        private Texture CreateTextureFromColor(Byte4 color, EngineResourceAllocator g)
        {
            if (!colorTextures.ContainsKey(color))
            {
                Texture colorTex = g.CreateTexture(2, 2, new Byte4[] { color, color, color, color });
                colorTextures[color] = colorTex;
            }

            return colorTextures[color];
        }

        public void OnFileLoaded(int requestID, string filePath, string loadedText) { }
    }

    internal class TextureFile
    {
        public TextureFile() { }

        public Texture Texture;
        public int RefCount;
        public byte[] SrcBytes; // cached file bytes from disk
        public byte[] DecodedRGBA; // cached decoded pixel bytes, used when the file cannot be loaded directly
        public int SrcWidth, SrcHeight;
        public bool Loaded; // set to true when this file is loaded from disk

        public bool ContainsDecodedData
        {
            get { return DecodedRGBA != null; }
        }

        public void ClearCache()
        {
            SrcBytes = null;
            DecodedRGBA = null;
        }
    }

    internal class DynamicTexture
    {
        public DynamicTexture() { }

        public Texture Texture;
        public int RefCount;
    }

    internal struct UnusedTexture
    {
        public Texture Resource;
        public int FromFrame;
    }

}
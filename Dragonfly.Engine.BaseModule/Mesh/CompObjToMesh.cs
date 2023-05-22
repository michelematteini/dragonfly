using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    internal class CompObjToMesh : Component, ICompUpdatable
    {
        private ConcurrentDictionary<ObjFile, ObjParsingArgs> parsingQueue;
        private Queue<ObjMeshLoadingArgs> loadingQueue;
        private Dictionary<string, CompMeshGeometry> objGeometryCache;
        private CompTimer gcEvent;
        private HashCode hashGen;

        // index arrays to decompose faces
        private int[/*ORIENTATION*/][/*POLYGON SIDE COUNT*/][/*INDICES*/] tessIndices;
		private const int MAX_FACE_EDGES = 10;
		private const int KEEP_ORIENTATION = 0;
		private const int CHANGE_ORIENTATION = 1;

        public UpdateType NeededUpdates
        {
            get
            {
                lock(loadingQueue)
                {
                    return (loadingQueue.Count > 0) ? UpdateType.FrameStart1 : UpdateType.None;
                }
            }
        }

        public bool IsLoading
        {
            get { return loadingQueue.Count > 0 || parsingQueue.Count > 0; }
        }

        public int MeshPerFrameLimit { get; set; }

        public CompObjToMesh(Component owner) : base(owner)
        {
            parsingQueue = new ConcurrentDictionary<ObjFile, ObjParsingArgs>();
            loadingQueue = new Queue<ObjMeshLoadingArgs>();
            objGeometryCache = new Dictionary<string, CompMeshGeometry>();
            MeshPerFrameLimit = 3;
            SetupTessellationIndices();
            gcEvent = new CompTimer(owner, 10.0f, DeleteUnusedGeometry);
            hashGen = new HashCode();
        }
		
		private void SetupTessellationIndices()
		{
			// fill with empty index array (that will skip that polygon)
			tessIndices = new int[2][][];
			tessIndices[KEEP_ORIENTATION] = new int[MAX_FACE_EDGES][];
			tessIndices[CHANGE_ORIENTATION] = new int[MAX_FACE_EDGES][];
			int[] unsupported = new int[] { };
			for(int i = 0; i < MAX_FACE_EDGES; i++) 
			{
				tessIndices[KEEP_ORIENTATION][i] = unsupported;
				tessIndices[CHANGE_ORIENTATION][i] = unsupported;
			}
			
			/* setup supported faces indices */
			
			// triagle indices
			tessIndices[KEEP_ORIENTATION][3] = new int[] {2, 1, 0};
			tessIndices[CHANGE_ORIENTATION][3] = new int[] {0, 1, 2};
			
			// quad indices
			tessIndices[KEEP_ORIENTATION][4] = new int[] {1, 0, 2, 2, 0, 3};
			tessIndices[CHANGE_ORIENTATION][4] = new int[] {1, 2, 0, 0, 2, 3};
		}

        private void DeleteUnusedGeometry()
        {
            lock (loadingQueue)
            {
                if (IsLoading) return; // do not garbage collect during loading!

                // collect references
                HashSet<string> referencedGeoms = new HashSet<string>();
                foreach(CompMesh mesh in GetComponents<CompMesh>())
                {
                    if (!mesh.Editable && mesh.Geometry is CompMeshGeometry g)
                        referencedGeoms.Add(g.Guid);
                }

                // free unused
                string[] geomGuids = new string[objGeometryCache.Count];
                objGeometryCache.Keys.CopyTo(geomGuids, 0);
                for (int i = 0; i < geomGuids.Length; i++)
                {
                    if (referencedGeoms.Contains(geomGuids[i])) continue; // referenced, in use

                    // delete mesh
                    objGeometryCache[geomGuids[i]].Dispose();
                    objGeometryCache.Remove(geomGuids[i]);
                }
            }
        }

        public void ParseAsync(string objPath, ObjParsingArgs args, CompMeshList destinationMesh)
        {
            args.DestinationMesh = destinationMesh;

            // validate args
            if (args.DestinationMesh == null)
                throw new ArgumentException("A destination mesh is needed to start loading the obj file!");

            // provide a default factory for the material if no material is specified from the user.
            if (args.Material == null && args.MaterialFactory == null)
                args.MaterialFactory = new CompMtlBasic.Factory { MaterialClass = Context.GetModule<BaseMod>().Settings.MaterialClasses.Solid };

            // load obj file
            objPath = args.DestinationMesh.Context.GetResourcePath(objPath);
            ObjFile objFile = new ObjFile();
            parsingQueue[objFile] = args;
            objFile.LoadFromFile(objPath, ObjFile_LoadingComplete);
        }


        private void ObjFile_LoadingComplete(ObjFile obj)
        {
            ObjParsingArgs args;
            if (!parsingQueue.TryRemove(obj, out args))
                return; // should never reach this point

            if (args.DestinationMesh.Disposed || args.DestinationMesh.Context.Released) return;

            // convert the loaded data to a mesh components
            for (int gi = 0; gi < obj.Groups.Count; gi++)
            {
                int nextSplitIndex = -1;
                ObjMeshLoadingArgs loadingArgs = new ObjMeshLoadingArgs();
                loadingArgs.ObjMaterial = obj.Groups[gi].Material;
                loadingArgs.MaterialFactory = args.MaterialFactory;
                loadingArgs.DestinationMesh = args.DestinationMesh;
                loadingArgs.OnMeshLoaded = args.OnMeshLoaded;
                loadingArgs.Material = args.Material;


                // prepare split data
                List<VertexTexNorm> vertices = null;
                List<ushort> indices = null;
                Dictionary<int, ushort> uniqueVertexIndices = null;
                
                Action PrepareNextSplit = () =>
                {
                    nextSplitIndex++;
                    loadingArgs.CacheGuid = string.Format("{0}-{1}-{2}", obj.FilePath, obj.Groups[gi].Name, nextSplitIndex);
                    vertices = new List<VertexTexNorm>();
                    indices = new List<ushort>();
                    uniqueVertexIndices = new Dictionary<int, ushort>();
                };

                PrepareNextSplit();

                // check for cached or loading geometry
                lock (loadingQueue)
                {
                    bool alreadyLoading = false;
                    while (objGeometryCache.ContainsKey(loadingArgs.CacheGuid))
                    {
                        // loaded or already loading, just ask for that instance
                        loadingQueue.Enqueue(loadingArgs);
                        alreadyLoading = true;
                        PrepareNextSplit();
                    }

                    if (alreadyLoading)
                        continue;

                    objGeometryCache[loadingArgs.CacheGuid] = null; // placeholder to flag as loading
                }

                int orientation = args.ChangeFaceOrientation != (loadingArgs.ObjMaterial.Cull == "invert") ? CHANGE_ORIENTATION : KEEP_ORIENTATION;
                
                IList<ObjFace> faces = obj.Groups[gi].Faces;

                // for each face
                for (int fi = 0; fi < faces.Count; fi++)
                {
                    ObjFace f = faces[fi];
                    int[] faceIndices = tessIndices[orientation][f.Vertices.Length];

                    // for each vertex
                    for (int vi = 0; vi < faceIndices.Length; vi++)
                    {
                        ObjVertex v = f.Vertices[faceIndices[vi]];
                        hashGen.Reset();
                        hashGen.Add(v.VertexIndex);
                        hashGen.Add(v.TexCoordIndex);
                        hashGen.Add(v.NormalIndex);
                        // TODO: take smoothing group into account
                        int vhash = hashGen.Resolve();

                        if (!uniqueVertexIndices.ContainsKey(vhash))
                        {
                            VertexTexNorm vertex = new VertexTexNorm();

                            // add position, normal tex coords
                            vertex.Position = obj.Vertices[v.VertexIndex - 1];
                            if (v.TexCoordIndex != 0)
                            {
                                vertex.TexCoords = obj.TexCoords[v.TexCoordIndex - 1];
                                vertex.TexCoords.Y = 1.0f - vertex.TexCoords.Y;
                            }
                            vertex.Normal = obj.Normals[v.NormalIndex - 1];
                            
                            // add the new vertex to the list
                            ushort vertexindex = (ushort)vertices.Count;
                            vertices.Add(vertex);
                            uniqueVertexIndices[vhash] = vertexindex;
                        }

                        indices.Add(uniqueVertexIndices[vhash]);
                    }

                    if((vertices.Count + 4) > CompMeshGeomBuffers.MAX_VERTEX_COUNT || (indices.Count + 4) > CompMeshGeomBuffers.MAX_INDEX_COUNT || fi == (faces.Count - 1) && vertices.Count > 0)
                    {
                        // queue mesh loading request
                        ObjMeshLoadingArgs splitArgs = loadingArgs;
                        splitArgs.Vertices = vertices;
                        splitArgs.Indices = indices;

                        // queue mesh loading request
                        lock (loadingQueue)
                        {
                            loadingQueue.Enqueue(splitArgs);
                        }

                        PrepareNextSplit();
                    }
                }
             
            }
        }

        public void Update(UpdateType updateType)
        {
            lock (loadingQueue)
            {
                List<ObjMeshLoadingArgs> waitingList = new List<ObjMeshLoadingArgs>();
                int loadedCount = 0;

                // create the requested meshes, up to <MeshPerFrameLimit> per frame
                while (loadedCount < MeshPerFrameLimit && loadingQueue.Count > 0)
                {
                    ObjMeshLoadingArgs meshToLoad = loadingQueue.Dequeue();
                    bool waitingForCache = meshToLoad.Vertices == null;
                    CompMeshGeometry meshGeometry;
                    bool cacheAvailable = objGeometryCache.TryGetValue(meshToLoad.CacheGuid, out meshGeometry) && meshGeometry != null;

                    if (waitingForCache && !cacheAvailable)
                    {
                        // waiting for a cache that is still not available, re-queue and try later
                        waitingList.Add(meshToLoad);
                        continue;
                    }

                    // create mesh geometry                
                    if (!waitingForCache)
                    {
                        // create mesh geometry and add it to the cache
                        meshGeometry = new CompMeshGeometry(this, meshToLoad.Vertices, meshToLoad.Indices);
                        meshGeometry.Guid = meshToLoad.CacheGuid;
                        meshGeometry.Name = meshGeometry.Guid;
                        objGeometryCache[meshToLoad.CacheGuid] = meshGeometry;
                    }

                    // create mesh material
                    CompMaterial meshMaterial = meshToLoad.Material;
                    if (meshToLoad.MaterialFactory != null)
                        meshMaterial = meshToLoad.MaterialFactory.CreateMaterial(meshToLoad.ObjMaterial.ToMaterialParams(), meshToLoad.DestinationMesh);

                    // parse custom mateial properties
                    if(meshToLoad.ObjMaterial.Cull == "none")
                        meshMaterial.CullMode = CullMode.None;

                    // add mesh to the list
                    CompMesh loadedMesh = meshToLoad.DestinationMesh.AddMesh(meshGeometry, meshMaterial);

                    // callback if specified
                    if (meshToLoad.OnMeshLoaded != null)
                        meshToLoad.OnMeshLoaded(loadedMesh);

                    loadedCount++;
                }

                // add back requests that were waiting for cache
                foreach (ObjMeshLoadingArgs waitingMesh in waitingList)
                    loadingQueue.Enqueue(waitingMesh);
            }

        }
    }

    public struct ObjParsingArgs
    {
        /// <summary>
        /// If true, face windings are inverted on loading.
        /// </summary>
        public bool ChangeFaceOrientation;
        /// <summary>
        /// The target mesh list that will be used as a container for all the loaded meshes.
        /// </summary>
        internal CompMeshList DestinationMesh;
        /// <summary>
        /// A material factory used to create the mesh material.
        /// </summary>
        public MaterialFactory MaterialFactory;
        /// <summary>
        /// An override material that will be assigned to the mesh. This is only used if a material factory is missing.
        /// </summary>
        public CompMaterial Material;
        /// <summary>
        /// A function that is called once the mesh has been completely loaded.
        /// </summary>
        public Action<CompMesh> OnMeshLoaded;
    }

    internal struct ObjMeshLoadingArgs
    {
        public List<VertexTexNorm> Vertices;
        public List<ushort> Indices;
        public ObjMaterial ObjMaterial;
        public MaterialFactory MaterialFactory;
        public CompMaterial Material; // used when factory is missing
        public CompMeshList DestinationMesh;
        public Action<CompMesh> OnMeshLoaded;
        public string CacheGuid;
    }

}
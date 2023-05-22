using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System;
using System.IO;

namespace Dragonfly.BaseModule
{

    public class CompTextureRef : Component<GraphicSurface>
    {
        private long lastUpdateFrame; // the last frame in which this texture was updated

        /// <summary>
        /// While this flag is set, setting the source of a texture reference cause the engine to load textures synchronously (i.e. without distributing the loading of textures over multiple frames).
        /// </summary>
        public static bool LoadSyncronously { get; set; }

        internal bool LoadNow { get; private set; }

        /// <summary>
        /// Type of source from which this texture is loading.
        /// </summary>
        internal TexRefSource Source { get; private set; }

        /// <summary>
        /// File Path from which this texture is loading.
        /// </summary>
        internal string SrcPath { get; private set; }

        /// <summary>
        /// Bitmap from which this texture is loading.
        /// </summary>
        internal System.Drawing.Bitmap SrcBitmap { get; private set; }

        /// <summary>
        /// Creation params that will be used to create this texture.
        /// </summary>
        internal TexCreationParams SrcParams { get; private set; }

        /// <summary>
        /// Render target reference used to initialize this texture.
        /// </summary>
        internal RenderTargetRef LoadedRtRef { get; private set; }

        // parameters from which this texture has been loaded from.

        internal TexRefSource LoadedSource { get; private set; }

        internal string LoadedSrcPath { get; private set; }

        internal TexCreationParams LoadedParams { get; private set; }

        internal Texture TexValue { get; private set; }

        internal TexRefFlags Flags { get; private set; }


        public CompTextureRef(Component owner) : this(owner, Color.Gray) { }

        public CompTextureRef(Component owner, Byte4 placeholderColor) : base(owner)
        {
            PlaceholderColor = placeholderColor;
            GetComponent<CompTextureLoader>().RequestPlaceholder(this);
        }

        internal void OnLoadingCompleted(Texture loadedTex)
        {
            LoadNow = false;
            TexValue = loadedTex;
            LoadedSource = Source;
            LoadedSrcPath = SrcPath;
            LoadedParams = SrcParams;
            IsPlaceholder = false;
            lastUpdateFrame = Context.Time.FrameIndex;
        }

        internal void OnPlaceholderCreated(Texture placeholderTex)
        {
            TexValue = placeholderTex;

            if (LoadedSource != TexRefSource.RenderBuffer)
            // render buffer can be immediately assigned, but still not be available,
            // this avoid overriding state while the render target is loading, but a placeholder is still provided.
            {
                LoadedSource = TexRefSource.None;
                IsPlaceholder = true;
            }

            lastUpdateFrame = Context.Time.FrameIndex;
        }

        internal void OnLoadedValueReleased()
        {
            LoadedSrcPath = string.Empty;
            LoadedParams = new TexCreationParams();
            LoadedSource = TexRefSource.None;
            LoadNow = false;
            TexValue = null;
            lastUpdateFrame = Context.Time.FrameIndex;
        }

        public Int2 Resolution
        {
            get
            {
                if (!Available)
                    return Int2.Zero;

                return Source == TexRefSource.RenderBuffer ? LoadedRtRef.GetValue().Resolution : TexValue.Resolution;
            }
        }

        public Byte4 PlaceholderColor { get; set; }

        public bool IsPlaceholder { get; private set; }

        /// <summary>
        /// Returns true if the value currently loaded in this texture reference contains HDR colors.
        /// </summary>
        public bool IsHdr 
        { 
            get
            {
                if (!Available)
                    return false;

                switch (Source)
                {
                    case TexRefSource.File:
                        {
                            if (string.IsNullOrEmpty(LoadedSrcPath))
                                return false;
                            string fileExt = Path.GetExtension(LoadedSrcPath).DefaultIfNull("").ToLower();
                            return fileExt == HdrFile.Extension;
                        }
                    default: 
                        return Flags == TexRefFlags.HdrColor;
                }
                
            }
        }


        /// <summary>
        /// Returns true if a valid texture value is ready for this reference.
        /// </summary>
        public bool Available
        {
            get { return getValue() != null; }
        }

        /// <summary>
        /// Returns true if an user value is available this reference and no loading is in progress. 
        /// </summary>
        public bool Loaded
        {
            get { return Available && Source == LoadedSource; }
        }

        /// <summary>
        /// Return true if this texture has just been loaded this frame to an user value.
        /// </summary>
        public bool LoadedChanged
        {
            get { return Loaded && ValueChanged; }
        }

        public void SetSource(string path)
        {
            GetComponent<CompTextureLoader>().StopLoading(this);
            SrcBitmap = null;
            SrcPath = Context.GetResourcePath(path);
            Source = TexRefSource.File;
            LoadNow = LoadSyncronously;
            GetComponent<CompTextureLoader>().BeginLoading(this);
        }

        public void SetSource(System.Drawing.Bitmap bitmap)
        {
            GetComponent<CompTextureLoader>().StopLoading(this);
            SrcBitmap = bitmap;
            Source = TexRefSource.Bitmap;
            LoadNow = LoadSyncronously;
            GetComponent<CompTextureLoader>().BeginLoading(this);
        }

        public void SetSource(RenderTargetRef renderTarget, TexRefFlags flags, bool makeRtCopy = false)
        {
            if (makeRtCopy)
            {
                TexCreationParams texParams = new TexCreationParams();
                texParams.Resolution.Width = renderTarget.GetValue().Width;
                texParams.Resolution.Height = renderTarget.GetValue().Height;
                texParams.Format = renderTarget.GetValue().Format;
                texParams.TextureInitializer = texture =>
                {
                    renderTarget.GetValue().CopyToTexture(texture);
                };
                SetSource(texParams);
            }
            else
            {
                GetComponent<CompTextureLoader>().StopLoading(this);
                SrcBitmap = null;
                LoadedRtRef = renderTarget;
                Flags = flags;
                Source = TexRefSource.RenderBuffer;
                LoadedSource = TexRefSource.RenderBuffer;
                IsPlaceholder = false;
                lastUpdateFrame = Context.Time.FrameIndex;
            }
        }

        public void SetSource(RenderTargetRef renderTarget)
        {
            SetSource(renderTarget, TexRefFlags.None);
        }

        /// <summary>
        /// Set as source a set of params that specify how to create this texture.
        /// </summary>
        public void SetSource(TexCreationParams texParams)
        {
            GetComponent<CompTextureLoader>().StopLoading(this);
            SrcBitmap = null;
            if(texParams.ID == 0)
                texParams.ID = TexCreationParams.NEXT_ID++;
            SrcParams = texParams;
            Flags = texParams.Flags;
            Source = TexRefSource.Dynamic;
            LoadNow = CompTextureRef.LoadSyncronously;
            GetComponent<CompTextureLoader>().BeginLoading(this);
        }

        /// <summary>
        /// Copy the source of this reference from another.
        /// <para/> If the specified reference is empty, this call do not affect the current source of this reference.
        /// </summary>
        public void SetSource(CompTextureRef texRef)
        {
            switch (texRef.Source)
            {
                case TexRefSource.File:
                    SetSource(texRef.SrcPath);
                    break;
                case TexRefSource.Bitmap:
                    SetSource(texRef.SrcBitmap);
                    break;
                case TexRefSource.RenderBuffer:
                    SetSource(texRef.LoadedRtRef);
                    break;
                case TexRefSource.Dynamic:                 
                    SetSource(texRef.SrcParams);
                    break;

                case TexRefSource.None:
                    if(LoadedSource == TexRefSource.None)
                    {
                        // if both are empty, copy over the placeholder color (available or not)
                        PlaceholderColor = texRef.PlaceholderColor;
                        TexValue = texRef.TexValue;
                        IsPlaceholder = texRef.IsPlaceholder;
                    }
                    break;
            }
        }

        public override bool ValueChanged
        {
            get
            {
                return lastUpdateFrame == (Context.Time.FrameIndex - 1);
            }
        }

        public override string ToString()
        {
            return string.Format("CompTextureRef: Src:{0}, {1}{2}",
                Source,
                Source == TexRefSource.File ? SrcPath : "",
                IsPlaceholder ? "Color " + PlaceholderColor : ""
            );
        }

        protected override GraphicSurface getValue()
        {
            if (LoadedSource == TexRefSource.RenderBuffer && LoadedRtRef.Available)
                return LoadedRtRef.GetValue();
            else
                return TexValue;
        }

        protected override void OnDispose()
        {
            try
            {
                if (!Context.Released)
                {
                    // stop any loading in progess
                    CompTextureLoader loader = GetComponent<CompTextureLoader>();
                    if (loader != null && !loader.Disposed)
                    {
                        loader.StopLoading(this);
                        loader.ReleaseLoadedValue(this);
                    }
                    
                    SrcBitmap = null;
                    Source = TexRefSource.None;
                }
            }
            finally { base.OnDispose(); }
        }
    }

    public enum TexRefSource
    {
        None = 0,
        File,
        Bitmap,
        RenderBuffer,
        Dynamic // the texture is newly created and not loaded
    }

    public struct TexCreationParams
    {
        internal static int NEXT_ID = 1;

        internal int ID;
        public Int2 Resolution;
        public SurfaceFormat Format;
        public TexRefFlags Flags;
        public Action<Texture> TextureInitializer;
    }

    public enum TexRefFlags
    {
        None,
        /// <summary>
        /// The texture content should be interpreted as an RGBE hdr color.
        /// </summary>
        HdrColor
    }

    public static class TextureRefHelpers
    {
        /// <summary>
        /// Set this texture reference to the shader if available, or do nothing if unavailable.
        /// </summary>
        public static void SetParam(this Shader s, string name, CompTextureRef value)
        {
            if (value == null || !value.Available) return;

            if (value.LoadedSource == TexRefSource.RenderBuffer && value.LoadedRtRef.Available)
                s.SetParam(name, value.LoadedRtRef.GetValue());
            else
                s.SetParam(name, value.TexValue);
        }

        /// <summary>
        /// Set this texture reference to the shader if available, or do nothing if unavailable.
        /// </summary>
        public static void SetParam(this EngineGlobals globals, string name, CompTextureRef value)
        {
            if (value == null || !value.Available) return;

            if (value.LoadedSource == TexRefSource.RenderBuffer && value.LoadedRtRef.Available)
                globals.SetParam(name, value.LoadedRtRef.GetValue());
            else
                globals.SetParam(name, value.TexValue);
        }
    }
}
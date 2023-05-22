using Dragonfly.Graphics.Math;
using System;
using System.Runtime.InteropServices;

namespace Dragonfly.Graphics.Resources
{
    public abstract class RenderTarget : GraphicSurface
    {
        protected internal RenderTarget(GraphicResourceID resID, SurfaceFormat format)
            : base(resID)
        {
            this.Format = format;
        }

        public override SurfaceFormat Format { get; protected set; }

        /// <summary>
        /// Send a request to save the data currently contained in this render target.
        /// <para/>
        /// </summary>
        public abstract void SaveSnapshot();

        /// <summary>
        /// Synchronously wait to get pixel data from this render target.
        /// </summary>
        public abstract void GetSnapshotData<T>(T[] destBuffer) where T : struct;

        /// <summary>
        /// Returns true if this render target is not currently in use, assigning its pixel data to the specified array.
        /// If the render target is in use, returns false and perform no operation.
        /// For this call to work SaveSnapshot() must be called first.
        /// If a null buffer is passed, this call just check if the buffer data is ready to be read.
        /// </summary>
        public abstract bool TryGetSnapshotData<T>(T[] destBuffer) where T : struct;

        /// <summary>
        /// Check if a snapshot previously requested by GetSnapshotData() is ready to be read.
        /// </summary>
        /// <returns></returns>
        public bool IsSnapshotReady()
        {
            return TryGetSnapshotData<byte>(null);
        }

        /// <summary>
        /// Copy pixel data from this render target to the specified texture.
        /// </summary>
        public abstract void CopyToTexture(Texture destTexture);

        /// <summary>
        /// Retrieve the data currently written on this render target that is then used to create a new bitmap image.
        /// </summary>
        public System.Drawing.Bitmap ToBitmap(bool alphaChannel = false)
        {
            System.Drawing.Bitmap rtSnapshot = null;
            GetDataAsBitmap(out rtSnapshot, true, alphaChannel);
            return rtSnapshot;
        }

        /// <summary>
        /// Retrieve the data currently written on this render target if already available that is then used to create a new bitmap image.
        /// If this target has not been rendered yet, this call return false and assign null to the provided image.
        /// For this call to work SaveSnapshot() must be called first.
        /// </summary>
        public bool TryGetSnapshotAsBitmap(out System.Drawing.Bitmap image, bool alphaChannel = false)
        {
            return GetDataAsBitmap(out image, false, alphaChannel);
        }

        private bool GetDataAsBitmap(out System.Drawing.Bitmap image, bool waitForGpu, bool alphaChannel)
        {
            image = null;

            // check for correct buffer format
            if (Format != SurfaceFormat.Color && Format != SurfaceFormat.AntialiasedColor)
                throw new InvalidOperationException("Only \"Color\" render targets can be converted to a System.Drawing.Bitmap!");

            // create a staging texture "snapshot" if we are synchronously reading
            if (waitForGpu) SaveSnapshot();

            // check that the snapshot is ready if we are not blocking waiting for it 
            if (!waitForGpu && !IsSnapshotReady())
                return false;

            // read snapshot data
            byte[] targetPixels = new byte[Width * Height * 4];
            GetSnapshotData<byte>(targetPixels);

            // clear alpha channel (if rendered to, usually contains junk that should not be copied to the image)
            if (!alphaChannel)
                RtBytesClearAlpha(targetPixels, 255);

            // convert snapshot to a bitmap and return it
            image = RtBytesToBitmap(targetPixels, Width, Height);
            return true;
        }

        internal static void RtBytesClearAlpha(byte[] targetPixels, byte alpha)
        {
            for (int i = 3; i < targetPixels.Length; i += 4)
                targetPixels[i] = alpha;
        }

        internal static System.Drawing.Bitmap RtBytesToBitmap(byte[] targetPixels, int width, int height)
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Drawing.Imaging.BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);

            Marshal.Copy(targetPixels, 0, bitmapData.Scan0, targetPixels.Length);
     
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        /// <summary>
        /// Assign to this instance another render target that will be used to perform depth testing.
        /// If a null value is specified, the current render target depth buffer will be used if available (default).
        /// </summary>
        public abstract void SetDepthWriteTarget(RenderTarget depthWriteTarget);


    }
}

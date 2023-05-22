using System.Drawing;
using System.Drawing.Imaging;

namespace Dragonfly.Utils
{
    public static class BitmapEx
    {
        /// <summary>
        /// Convet a grayscale bitmap to a float matrix. The output values are mapped from 0 to the specified multiplier.
        /// </summary>
        public static void ToFloatMatrix(this Bitmap bitmap, float[,] destHeightMap, float multiplier)
        {
            BitmapDataEx data = bitmap.Lock(ImageLockMode.ReadOnly);
            multiplier = multiplier / 255.0f;

            for (; data.RowIndex < data.Height; data.RowIndex++)
                for (; data.ColumnIndex < data.Width; data.ColumnIndex++)
                    destHeightMap[data.ColumnIndex, data.RowIndex] = data.Pixel.G * multiplier;

            bitmap.Unlock(data);           
        }

        public static BitmapDataEx Lock(this Bitmap bitmap, ImageLockMode lockMode)
        {
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), lockMode, bitmap.PixelFormat);
            return new BitmapDataEx(data, lockMode);
        }

        public static void Unlock (this Bitmap bitmap, BitmapDataEx data)
        {
            bitmap.UnlockBits(data.Data);
        }

    }

}

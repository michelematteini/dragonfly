using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class BitmapDataEx
{
    public class PixelData
    {
        private byte[] buffer;
        private bool hasAlpha;

        internal PixelData(byte[] buffer, bool hasAlpha)
        {
            this.buffer = buffer;
            this.hasAlpha = hasAlpha;
        }

        internal int Offset;

        public byte B 
        {
            get { return buffer[Offset + 0]; }
            set { buffer[Offset + 0] = value; }
        }

        public byte G
        {
            get { return buffer[Offset + 1]; }
            set { buffer[Offset + 1] = value; }
        }
        public byte R
        {
            get { return buffer[Offset + 2]; }
            set { buffer[Offset + 2] = value; }
        }

        public byte A
        {
            get { return hasAlpha ? buffer[Offset + 3] : byte.MaxValue; }
            set { if (hasAlpha) buffer[Offset + 3] = value; }
        }
    }

    private int rowIndex, columnIndex;
    private bool readEnabled, writeEnabled;

    internal BitmapDataEx(BitmapData data, ImageLockMode lockMode)
    {
        Data = data;
        ChannelCount = data.Stride / data.Width;
        RowData = new byte[(Width + 1) * ChannelCount];
        readEnabled = lockMode == ImageLockMode.ReadOnly || lockMode == ImageLockMode.ReadWrite;
        writeEnabled = lockMode == ImageLockMode.WriteOnly || lockMode == ImageLockMode.ReadWrite;
        Pixel = new PixelData(RowData, ChannelCount > 3);
        if (readEnabled)
            ReadRowData();
    }

    public BitmapData Data { get; private set; }

    public int Width { get { return Data.Width; } }

    public int Height { get { return Data.Height; } }

    public int ChannelCount { get; private set; }

    public byte[] RowData { get; private set; }

    public int RowIndex
    {
        get
        {
            return rowIndex;
        }
        set
        {
            if (value != rowIndex)
            {
                if (writeEnabled)
                    WriteRowData();
                rowIndex = value;
                if (readEnabled)
                    ReadRowData();
                ColumnIndex = 0;
            }
        }
    }

    public int ColumnIndex
    {
        get { return columnIndex; }
        set
        {
            columnIndex = value;
            Pixel.Offset = columnIndex * ChannelCount;
        }
    }

    public int PixelIndex { get { return columnIndex + Width * rowIndex; } }

    public PixelData Pixel { get; private set; }

    private void ReadRowData()
    {
        if (rowIndex >= 0 && rowIndex < Height - 1)
            Marshal.Copy(new IntPtr(Data.Scan0.ToInt64() + rowIndex * Data.Stride), RowData, 0, RowData.Length - ChannelCount);
    }

    private void WriteRowData()
    {
        if (rowIndex >= 0 && rowIndex < Height - 1)
            Marshal.Copy(RowData, 0, new IntPtr(Data.Scan0.ToInt64() + rowIndex * Data.Stride), RowData.Length - ChannelCount);
    }

}
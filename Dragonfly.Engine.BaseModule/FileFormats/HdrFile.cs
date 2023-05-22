using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.IO;

namespace Dragonfly.BaseModule
{
    public class HdrFile
    {
        public const string Extension = ".hdr";
        private const string HDR_FILE_TOKEN = "#?RADIANCE";

        private byte[] colorBytes;

        public HdrFile(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open);
            BinaryReader reader = new BinaryReader(fs);
            LoadHeader(reader);
            colorBytes = new byte[fs.Length - fs.Position];
            fs.Read(colorBytes, 0, colorBytes.Length);
            IsCompressed = ReadScanlineType(0) != ScanlineType.Uncompressed; // writing on compressed files not currently supported
        }

        /// <summary>
        /// Create a new uncompressed hdr file of the given size. 
        /// </summary>
        public HdrFile(int width, int height)
        {
            // create file header
            {
                FileHeader header = new FileHeader();
                header.Exposure = 1;
                header.Format = "32-bit_rle_rgbe";
                header.Gamma = 1;
                header.Width = width;
                header.Height = height;
                Header = header;
            }

            // create an empty file byte array with the header 
            colorBytes = new byte[width * height * 4];
        }


        private void LoadHeader(BinaryReader reader)
        {
            string headerLine = reader.ReadASCIILine();

            if (headerLine != HDR_FILE_TOKEN)
                throw new InvalidDataException("The specified file is not a valid .hdr file!");

            FileHeader header = new FileHeader();
            header.Gamma = 1.0f;
            header.Exposure = 1.0f;
            for (; true; headerLine = reader.ReadASCIILine().Trim())
            {
                if (headerLine == null)
                    throw new InvalidDataException("Corrupted file header.");

                // skip header comments
                if (headerLine.StartsWith("#"))
                    continue;

                if (headerLine.Contains("="))
                {
                    // read header values 
                    string[] hlineElems = headerLine.Split('=');

                    switch (hlineElems[0].Trim().ToUpper())
                    {
                        case "GAMMA":
                            header.Gamma = hlineElems[1].ParseInvariantFloat();
                            break;

                        case "FORMAT":
                            header.Format = hlineElems[1];
                            break;
                        case "EXPOSURE":
                            header.Exposure = hlineElems[1].ParseInvariantFloat();
                            break;
                    }
                }
                else if (headerLine == string.Empty)
                {
                    // header end, read resolution and stop
                    headerLine = reader.ReadASCIILine().Trim();

                    string[] hlineElems = headerLine.Split(' ');
                    if (hlineElems.Length != 4)
                        throw new InvalidDataException("Corrupted file header: invalid resolution.");

                    header.Width = hlineElems[3].ParseInvariantInt();
                    header.Height = hlineElems[1].ParseInvariantInt();
                    break;
                }
            }

            Header = header;
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.WriteASCIILine(HDR_FILE_TOKEN);
            writer.WriteASCIILine("# Made with Dragonfly Engine");
            writer.WriteASCIILine("GAMMA=" + Header.Gamma);
            writer.WriteASCIILine("FORMAT=32-bit_rle_rgbe");
            writer.WriteASCIILine("");
            writer.WriteASCIILine(string.Format("-Y {1} +X {0}", Header.Width, Header.Height));
        }

        public FileHeader Header { get; private set; }

        public bool IsCompressed { get; private set; }

        public int PixelCount
        {
            get
            {
                return Header.Width * Header.Height;
            }
        }

        /// <summary>
        /// Copy this image hdr color data into a 96bpp float buffer in an interleaved R-G-B pattern.
        /// </summary>
        public void CopyHdrDataTo(float[] destRgbPixels)
        {
            if (destRgbPixels.Length != Header.Width * Header.Height * 3)
                throw new ArgumentException("The specified buffer is of an invalid length (should be equal to width * height * 3)");

            // read  and decode all image scanlines
            byte[] destRawPixels = new byte[Header.Width * 4];
            for (int y = 0, dataOffset = 0, rgbOffset = 0; y < Header.Height; y++, rgbOffset += 3 * Header.Width)
            {
                ReadScanlineTo(destRawPixels, 0, ref dataOffset);
                DecodeScanline(destRawPixels, 0, destRgbPixels, rgbOffset, 1 / Header.Exposure);
            }
        }

        /// <summary>
        /// Copy this image color data into a 32bpp color buffer in an interleaved B-G-R-A pattern, encoding hdr values.
        /// </summary>
        public void CopyRgbDataTo(byte[] destRgbPixels, HdrColorEncoder encoder)
        {
            if (destRgbPixels.Length != Header.Width * Header.Height)
                throw new ArgumentException("The specified buffer is of an invalid length (should be equal to width * height)");

            // read  and decode all image scanlines
            byte[] destRawPixels = new byte[Header.Width * 4];
            float[] destHdrPixels = new float[Header.Width * 3];
            for (int y = 0, dataOffset = 0, rgbOffset = 0; y < Header.Height; y++, rgbOffset += 4 * Header.Width)
            {
                ReadScanlineTo(destRawPixels, 0, ref dataOffset);
                DecodeScanline(destRawPixels, 0, destHdrPixels, 0, 1 / Header.Exposure);
                encoder(destHdrPixels, 0, destHdrPixels.Length, destRgbPixels, rgbOffset);
            }
        }

        /// <summary>
        /// Copy this image color data into a RGBE 32bit color buffer in an interleaved R-G-B-E pattern.
        /// </summary>
        public void CopyRGBEDataTo(byte[] destRgbePixels)
        {
            if (destRgbePixels.Length != Header.Width * Header.Height * 4)
                throw new ArgumentException("The specified buffer is of an invalid length (should be equal to width * height * 4)");

            // read  and decode all image scanlines
            for (int y = 0, dataOffset = 0, rgbOffset = 0; y < Header.Height; y++, rgbOffset += Header.Width * 4)
                ReadScanlineTo(destRgbePixels, rgbOffset, ref dataOffset);
        }

        /// <summary>
        /// Decode the specified scanline from RGBE to float rgb hdr values.
        /// </summary>
        private void DecodeScanline(byte[] srcRawPixels, int srcOffset, float[] destHdrPixels, int destHdrOffset, float multiplier)
        {
            int destPixelEnd = destHdrOffset + Header.Width * 3;
            RGBE.Decoder(srcRawPixels, srcOffset, srcOffset + Header.Width * 4, destHdrPixels, destHdrOffset);

            if (!multiplier.AlmostEquals(1)) // apply a scale if required
                for (int i = destHdrOffset; i < destPixelEnd; i++)
                    destHdrPixels[i] *= multiplier;
        }

        /// <summary>
        /// Encode the specified scanline from rgb hdr value to RGBE bytes.
        /// </summary>
        private void EncodeScanline(float[] srcHdrPixels, int srcOffset, byte[] destRgbeData, int destOffset)
        {
            RGBE.Encoder(srcHdrPixels, srcOffset, srcOffset + Header.Width * 3, destRgbeData, destOffset);
        }

        /// <summary>
        /// Read a scanline to the specified byte buffer in the native RGBE format.
        /// </summary>
        private void ReadScanlineTo(byte[] destRawPixels, int destOffset, ref int fileBytesOffset)
        {
            ScanlineType scType = ReadScanlineType(ref fileBytesOffset);

            switch (scType)
            {
                case ScanlineType.Uncompressed:
                    int stride = Header.Width * 4;
                    Array.Copy(colorBytes, fileBytesOffset, destRawPixels, destOffset, stride);
                    fileBytesOffset += stride;
                    break;

                case ScanlineType.NewRunLenght:
                    for (int channelID = 0; channelID < 4; channelID++)
                        for (int readPixCount = 0; readPixCount < Header.Width;)
                        {
                            int seqLen = colorBytes[fileBytesOffset++];
                            int isValueDump = seqLen > 128 ? 0 : 1; // differentiate between value sequences (dumps) and repeated values
                            seqLen = seqLen * isValueDump + (seqLen & 0x7F) * (1 - isValueDump);
                            int curOffset = destOffset + channelID + readPixCount * 4;
                            readPixCount += seqLen;

                            for (int i = 0; i < seqLen; i++, curOffset += 4)
                            {
                                destRawPixels[curOffset] = colorBytes[fileBytesOffset];
                                fileBytesOffset += isValueDump; // move to the next value if is not repeated
                            }

                            fileBytesOffset += (1 - isValueDump); // move after the sequence value
                        }
                    break;

                case ScanlineType.LegacyRunLength:
                    throw new Exception("Unsupported file format!");

                default: 
                    throw new Exception("Invalid file format!");
            }
        }

        private void WriteScanline(byte[] srcRawPixels, int srcOffset, ref int fileBytesOffset)
        {
            int stride = Header.Width * 4;
            Array.Copy(srcRawPixels, srcOffset, colorBytes, fileBytesOffset, stride);
            fileBytesOffset += stride;
        }

        ScanlineType ReadScanlineType(ref int fileDataOffset)
        {
            ScanlineType scType = ReadScanlineType(fileDataOffset);
            if (scType != ScanlineType.Uncompressed)
                fileDataOffset += 4;
            return scType;
        }

        ScanlineType ReadScanlineType(int fileDataOffset)
        {
            if (colorBytes[fileDataOffset] == 1 && colorBytes[fileDataOffset + 1] == 1 && colorBytes[fileDataOffset + 2] == 1)
                return ScanlineType.LegacyRunLength;
            if (colorBytes[fileDataOffset] == 2 && colorBytes[fileDataOffset + 1] == 2)
                return ScanlineType.NewRunLenght;

            return ScanlineType.Uncompressed;
        }

        public void Save(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                BinaryWriter writer = new BinaryWriter(fs);
                WriteHeader(writer);
                writer.Write(colorBytes);
            }
        }

        private void ThrowIfCompressed()
        {
            if (IsCompressed)
                throw new Exception("Unsupported operation on a compressed image!");
        }

        public void SetPixel(uint x, uint y, Float3 rgb)
        {
            ThrowIfCompressed();

            Byte4 rgbe = ColorEncoding.EncodeHdr(rgb, RGBE.Encoder);
            long pixelOffset = 4 * (x + y * Header.Width);
            colorBytes[pixelOffset + 0] = rgbe.R;
            colorBytes[pixelOffset + 1] = rgbe.G;
            colorBytes[pixelOffset + 2] = rgbe.B;
            colorBytes[pixelOffset + 3] = rgbe.A;
        }

        public Float3 GetPixel(uint x, uint y)
        {
            ThrowIfCompressed();

            long pixelOffset = 4 * (x + y * Header.Width);
            Byte4 rgbe;
            rgbe.R = colorBytes[pixelOffset + 0];
            rgbe.G = colorBytes[pixelOffset + 1];
            rgbe.B = colorBytes[pixelOffset + 2];
            rgbe.A = colorBytes[pixelOffset + 3];

            return ColorEncoding.DecodeHdr(rgbe, RGBE.Decoder);
        }

        public void SetRGBEData(byte[] data)
        {
            ThrowIfCompressed();

            if (data.Length != Header.Width * Header.Height * 4)
                throw new Exception("The provided array is not of the right size!");

            Array.Copy(data, 0, colorBytes, 0, data.Length);
        }

        /// <summary>
        /// Set the pixel data of this image to the specified hdr rgb linear values
        /// </summary>
        public void SetHdrData(float[] srcRgbPixels)
        {
            ThrowIfCompressed();

            if (srcRgbPixels.Length != Header.Width * Header.Height * 3)
                throw new ArgumentException("The specified buffer is of an invalid length (should be equal to width * height * 3)");

            // encode all the image scanlines
            byte[] destRawPixels = new byte[Header.Width * 4];
            for (int y = 0, dataOffset = 0, rgbOffset = 0; y < Header.Height; y++, rgbOffset += 3 * Header.Width)
            {
                EncodeScanline(srcRgbPixels, rgbOffset, destRawPixels, 0);
                WriteScanline(destRawPixels, 0, ref dataOffset);
            }
        }

        /// <summary>
        /// returns a unsafe pointer to a writable array containing this picture RGBE color data.
        /// </summary>
        public byte[] GetRGBEDataPtr()
        {
            return colorBytes;
        }

        public struct FileHeader
        {
            public float Gamma;

            public string Format;

            public int Width, Height;

            public float Exposure;
        }

        private enum ScanlineType
        {
            Uncompressed,
            LegacyRunLength,
            NewRunLenght
        }
    }
}

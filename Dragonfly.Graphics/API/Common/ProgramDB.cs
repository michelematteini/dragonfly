using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.API.Common
{
    public class ProgramDB
    {
        public byte[] RawBytes { get; private set; }

        public ProgramDB(byte[] rawBytes)
        {
            RawBytes = rawBytes;
        }

        public ProgramDB(byte[][] programs)
        {
            if (programs.Length > 2 || programs.Length < 1)
                throw new NotSupportedException();

            // calc buffer size and the second program offset
            int mergedSize = 4 + programs[0].Length;
            int p2StartIndex = 0;
            if (programs.Length > 1)
            {
                mergedSize += programs[1].Length;
                p2StartIndex = 4 + programs[0].Length;
            }

            // save second program offset
            RawBytes = new byte[mergedSize];
            SaveBufferInt(RawBytes, 0, p2StartIndex);

            // save programs
            Array.Copy(programs[0], 0, RawBytes, 4, programs[0].Length); // save first program
            if (programs.Length > 1)
                Array.Copy(programs[1], 0, RawBytes, p2StartIndex, programs[1].Length); // save second program
        }


        private static void SaveBufferInt(byte[] buffer, int at, int value)
        {
            buffer[at + 0] = (byte)(value >> 24);
            buffer[at + 1] = (byte)(value >> 16);
            buffer[at + 2] = (byte)(value >> 8);
            buffer[at + 3] = (byte)(value);
        }

        private static int LoadBufferInt(byte[] buffer, int at)
        {
            int value = buffer[at++];
            value = (value << 8) + buffer[at++];
            value = (value << 8) + buffer[at++];
            value = (value << 8) + buffer[at++];
            return value;
        }

        public int GetProgramCount()
        {
            int p2Offset = LoadBufferInt(RawBytes, 0);

            return p2Offset == 0 ? 1 : 2;
        }

        public int GetProgramStartID(int progIndex)
        {
            if (progIndex == 0)
                return 4;
            else
                return LoadBufferInt(RawBytes, 0);
        }

        public int GetProgramSize(int progIndex)
        {
            int p2Offset = LoadBufferInt(RawBytes, 0);

            if (progIndex == 0)
                return p2Offset > 0 ? p2Offset - 4 : RawBytes.Length - 4;
            else
                return RawBytes.Length - p2Offset;
        }

    }
}

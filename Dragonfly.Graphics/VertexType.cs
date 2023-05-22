using Dragonfly.Utils;
using System;

namespace Dragonfly.Graphics
{
    public class VertexType
    {
        public static readonly VertexType Empty = new VertexType();

        private int[] offsets;
		private int hashCode;

        private static int[] ElementsToOffsets(VertexElement[] elems)
        {
            int[] offsets = new int[elems.Length + 1];
            int vertexOffset = 0, instaceOffset = 0;
            for (int i = 0; i < elems.Length; i++)
            {
                int elSize = 0;
                switch (elems[i])
                {
                    case VertexElement.Float:
                    case VertexElement.Float | VertexElement.InstanceStream:
                        elSize = 4;
                        break;
                    case VertexElement.Position2:
                    case VertexElement.Float2:
                    case VertexElement.Position2 | VertexElement.InstanceStream:
                    case VertexElement.Float2 | VertexElement.InstanceStream:
                        elSize = 8;
                        break;
                    case VertexElement.Position3:
                    case VertexElement.Float3:
                    case VertexElement.Position3 | VertexElement.InstanceStream:
                    case VertexElement.Float3 | VertexElement.InstanceStream:
                        elSize = 12;
                        break;
                    case VertexElement.Position4:
                    case VertexElement.Float4:
                    case VertexElement.Position4 | VertexElement.InstanceStream:
                    case VertexElement.Float4 | VertexElement.InstanceStream:
                        elSize = 16;
                        break;
                }

                if ((elems[i] | VertexElement.InstanceStream) == elems[i])
                {
                    offsets[i] = instaceOffset;
                    instaceOffset += elSize;
                }
                else
                {
                    offsets[i] = vertexOffset;
                    vertexOffset += elSize;
                }

                offsets[elems.Length] += elSize;
            }
            return offsets;
        }

		private static int ElementsToHashCode(VertexElement[] elems)
		{
            return HashCode.Combine<VertexElement>(elems);
		}
		
        public VertexType(params VertexElement[] elems)
        {
            this.Elements = elems;
            this.offsets = ElementsToOffsets(elems);
			this.hashCode = ElementsToHashCode(elems);
        }

        public VertexElement[] Elements { get; private set; }

        public int ByteSize
        {
            get
            {
                return offsets[Elements.Length];
            }
        }

        public int GetOffset(int elemIndex)
        {
            return offsets[elemIndex];
        }

		public override int GetHashCode()
		{
			return this.hashCode;
		}

        public override bool Equals(object obj)
        {
            VertexType otherVType = obj as VertexType;
            if (otherVType == null)
                return false;
            return otherVType.hashCode == hashCode;
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", Elements) + "}";
        }

    }

    /// <summary>
    /// Type and usage of a vertex element.
    /// </summary>
    [Flags]
    public enum VertexElement
    {
        ///<summary>Vertex position data, 2x 32bit floats </summary>
        Position2 = 1,
        ///<summary>Vertex position data, 3x 32bit floats</summary>
        Position3 = 2,
        ///<summary>Vertex position data, 4x 32bit floats</summary>
        Position4 = 4,
        ///<summary>Custom data values, 32bit float</summary>
        Float = 8,
        ///<summary>Custom data values, 2x 32bit floats</summary>
        Float2 = 16,
        ///<summary>Custom data values, 3x 32bit floats</summary>
        Float3 = 32,
        ///<summary>Custom data values, 4x 32bit floats</summary>
        Float4 = 64,
        ///<summary>Flag: the element is for instancing data stream</summary>
        InstanceStream = 1 << 30,
        ///<summary>Invalid value, for masking only.</summary>
        StreamMask = 3 << 30,
        ///<summary>Invalid value, for masking only.</summary>
        TypeMask = ~(3 << 30),
        ///<summary>Invalid value, for masking only.</summary>
        AllPositions = Position2 | Position3 | Position4
    }
}

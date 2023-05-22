using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public struct VertexTexNorm
    {
        public Float3 Position;
        public Float2 TexCoords;
        public Float3 Normal;

        public VertexTexNorm(Float3 position, Float2 texCoords, Float3 normal)
        {
            this.Position = position;
            this.TexCoords = texCoords;
            this.Normal = normal;
        }

        public static readonly VertexType VertexType = new VertexType(
            VertexElement.Position3,
            VertexElement.Float2,
            VertexElement.Float3
        );

        public override string ToString()
        {
            return string.Format("POS:{0} COORDS:{1} NRM:{2}", Position.ToString(), TexCoords.ToString(), Normal.ToString());
        }

    }

    public struct VertexPosition
    {
        public Float3 Position;

        public VertexPosition(Float3 position)
        {
            this.Position = position;
        }

        public static readonly VertexType VertexType = new VertexType(
            VertexElement.Position3
        );

        public override string ToString()
        {
            return Position.ToString();
        }

    }

}

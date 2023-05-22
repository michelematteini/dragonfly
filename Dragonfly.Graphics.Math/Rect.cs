using System.Xml.Serialization;

namespace Dragonfly.Graphics.Math
{
    /// <summary>
    /// A rectangle in 2d space.
    /// </summary>
    public struct Rect
    {
        public float X, Y;
        public float Width, Height;
        public float Rotation;

        public Rect(float x, float y, float width, float height, float rotation)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Rotation = rotation;
        }

        public Rect(float x, float y, float width, float height) : this(x, y, width, height, 0) { }

        public Rect(Float2 position, Float2 size, float rotation) : this(position.X, position.Y, size.X, size.Y, rotation) { }

        public Rect(Float2 position, Float2 size) : this(position.X, position.Y, size.X, size.Y, 0) { }

        public Rect(Float2 position, float size) : this(position.X, position.Y, size, size, 0) { }


        [XmlIgnore]
        public Float2 Position
        {
            get { return new Float2(X, Y); }
        }

        [XmlIgnore]
        public Float2 Size
        {
            get { return new Float2(Width, Height); }
        }

        [XmlIgnore]
        public Float2 WidthDirection
        {
            get { return Float2.FromAngle(Rotation); }
        }

        [XmlIgnore]
        public Float2 HeightDirection
        {
            get { return Float2.FromAngle(Rotation).Rotate90(); }
        }

        [XmlIgnore]
        public Float2 WidthVector
        {
            get { return Float2.FromAngle(Rotation) * Width; }
        }

        [XmlIgnore]
        public Float2 HeightVector
        {
            get { return Float2.FromAngle(Rotation).Rotate90() * Height; }
        }

        /// <summary>
        /// Return the coordinates of the specified point on this region.
        /// </summary>
        public Float2 GetCoordsAt(Float2 location)
        {
            return new Float2(
                (location - Position).Dot(WidthDirection) / Width,
                (location - Position).Dot(HeightDirection) / Height
            );
        }

        public override string ToString()
        {
            return string.Format("Pos:{0}, Size:{1}, Rotation:{2:F2}deg", Position, Size, Rotation.ToDegree());
        }

    }
}

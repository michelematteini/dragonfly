using System;

namespace Dragonfly.Graphics.Math
{
    public struct Float2x2
    {
        public static Float2x2 Scale(float scale)
        {
            return new Float2x2(scale, 0, 0, scale);
        }

        public static Float2x2 Scale(Float2 scale)
        {
            return new Float2x2(scale.X, 0, 0, scale.Y);
        }

        public static Float2x2 Rotation(float radians)
        {
            float sin = FMath.Sin(radians);
            float cos = FMath.Cos(radians);
            return new Float2x2(
                cos, -sin,
                sin, cos
            );
        }

        public float A11, A12;
        public float A21, A22;

        public Float2x2(
                float a11, float a12,
                float a21, float a22
            )
        {
            A11 = a11; A12 = a12;
            A21 = a21; A22 = a22;
        }

        public Float2x2(Float2 row1, Float2 row2) : this(row1.X, row1.Y, row2.X, row2.Y) { }

        public float Determinant
        {
            get
            {
                return A11 * A22 - A12 * A21;
            }
        }

        //Float2 x Float2x2 non commutative

        public static Float2 operator *(Float2 v, Float2x2 m)
        {
            return new Float2(                
                v.X * m.A11 + v.Y * m.A21,
                v.X * m.A12 + v.Y * m.A22
            );
        }

        //Float2x3 x Float2 non commutative

        public static Float2 operator *(Float2x2 m, Float2 v)
        {
            return new Float2(
                v.X * m.A11 + v.Y * m.A12,
                v.X * m.A21 + v.Y * m.A22
            );
        }

    }
}

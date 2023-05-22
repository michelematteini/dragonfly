using System;

namespace Dragonfly.Graphics.Math
{
    public struct Float3x3
    {
        public float A11, A12, A13;
        public float A21, A22, A23;
        public float A31, A32, A33;

        public Float3x3(
                float a11, float a12, float a13,
                float a21, float a22, float a23,
                float a31, float a32, float a33
            )
        {
            A11 = a11; A12 = a12; A13 = a13;
            A21 = a21; A22 = a22; A23 = a23;
            A31 = a31; A32 = a32; A33 = a33;
        }

        public Float3x3(Float4x4 m)
        {
            A11 = m.A11; A12 = m.A12; A13 = m.A13;
            A21 = m.A21; A22 = m.A22; A23 = m.A23;
            A31 = m.A31; A32 = m.A32; A33 = m.A33;
        }

        public Float3x3(Float3 row1, Float3 row2, Float3 row3) : this(row1.X, row1.Y, row1.Z, row2.X, row2.Y, row2.Z, row3.X, row3.Y, row3.Z) { }

        public static explicit operator Float3x3(Float4x4 m)
        {
            return new Float3x3(m);
        }

        public float Determinant
        {
            get
            {
                Float2x2 subx = new Float2x2(A22, A23, A32, A33);
                Float2x2 suby = new Float2x2(A21, A23, A31, A33);
                Float2x2 subz = new Float2x2(A21, A22, A31, A32);
                return A11 * subx.Determinant - A12 * suby.Determinant + A13 * subz.Determinant;
            }
        }

        public static Float3x3 Identity
        {
            get
            {
                return new Float3x3(
                    1, 0, 0,
                    0, 1, 0,
                    0, 0, 1
                );
            }
        }

        public Float3x3 Transpose()
        {
            return new Float3x3(
                A11, A21, A31,
                A12, A22, A32,
                A13, A23, A33
            );
        }

        public Float3 GetColumn(int index)
        {
            switch (index % 3)
            {
                default:
                case 0: return new Float3(A11, A21, A31);
                case 1: return new Float3(A12, A22, A32);
                case 2: return new Float3(A13, A23, A33);
            }
        }

        public Float3 GetRow(int index)
        {
            switch (index % 3)
            {
                default:
                case 0: return new Float3(A11, A12, A13);
                case 1: return new Float3(A21, A22, A23);
                case 2: return new Float3(A31, A32, A33);
            }
        }

        public void SetColumn(int index, Float3 value)
        {
            switch (index % 3)
            {
                default:
                case 0: A11 = value.X; A21 = value.Y; A31 = value.Z; break;
                case 1: A12 = value.X; A22 = value.Y; A32 = value.Z; break;
                case 2: A13 = value.X; A23 = value.Y; A33 = value.Z; break;
            }
        }

        public void SetRow(int index, Float3 value)
        {
            switch (index % 3)
            {
                default:
                case 0: A11 = value.X; A12 = value.Y; A13 = value.Z; break;
                case 1: A21 = value.X; A22 = value.Y; A23 = value.Z; break;
                case 2: A31 = value.X; A32 = value.Y; A33 = value.Z; break;
            }
        }

        //Float3 x Float3x3 non commutative

        public static Float3 operator *(Float3 v, Float3x3 m)
        {
            return new Float3(
                v.X * m.A11 + v.Y * m.A21 + v.Z * m.A31,
                v.X * m.A12 + v.Y * m.A22 + v.Z * m.A32,
                v.X * m.A13 + v.Y * m.A23 + v.Z * m.A33
            );
        }

        public static Float3 operator *(Float3x3 m, Float3 v)
        {
            return new Float3(
                v.X * m.A11 + v.Y * m.A12 + v.Z * m.A13,
                v.X * m.A21 + v.Y * m.A22 + v.Z * m.A23,
                v.X * m.A31 + v.Y * m.A32 + v.Z * m.A33
            );
        }

        //Float3x3 x Float3x3 non commutative

        public static Float3x3 operator *(Float3x3 m1, Float3x3 m2)
        {
            return new Float3x3(
                //row 1
                m1.A11 * m2.A11 + m1.A12 * m2.A21 + m1.A13 * m2.A31,
                m1.A11 * m2.A12 + m1.A12 * m2.A22 + m1.A13 * m2.A32,
                m1.A11 * m2.A13 + m1.A12 * m2.A23 + m1.A13 * m2.A33,
                //row 2
                m1.A21 * m2.A11 + m1.A22 * m2.A21 + m1.A23 * m2.A31,
                m1.A21 * m2.A12 + m1.A22 * m2.A22 + m1.A23 * m2.A32,
                m1.A21 * m2.A13 + m1.A22 * m2.A23 + m1.A23 * m2.A33,
                //row 3
                m1.A31 * m2.A11 + m1.A32 * m2.A21 + m1.A33 * m2.A31,
                m1.A31 * m2.A12 + m1.A32 * m2.A22 + m1.A33 * m2.A32,
                m1.A31 * m2.A13 + m1.A32 * m2.A23 + m1.A33 * m2.A33
            );
        }

        //Float3x3 x float
        public static Float3x3 operator *(Float3x3 m, float k)
        {
            return new Float3x3(
                m.A11 * k, m.A12 * k, m.A13 * k,
                m.A21 * k, m.A22 * k, m.A23 * k,
                m.A31 * k, m.A32 * k, m.A33 * k
            );
        }


        public static Float3x3 operator *(float k, Float3x3 m)
        {
            return m * k;
        }

        public static Float3x3 operator /(Float3x3 m, float k)
        {
            return m * (1 / k);
        }

        public Float3x3 Invert()
        {
            float det = Determinant;
            return new Float3x3(
                +new Float2x2(A22, A23, A32, A33).Determinant / det,
                -new Float2x2(A12, A13, A32, A33).Determinant / det,
                +new Float2x2(A12, A13, A22, A23).Determinant / det,
                -new Float2x2(A21, A23, A31, A33).Determinant / det,
                +new Float2x2(A11, A13, A31, A33).Determinant / det,
                -new Float2x2(A11, A13, A21, A23).Determinant / det,
                +new Float2x2(A21, A22, A31, A32).Determinant / det,
                -new Float2x2(A11, A12, A31, A32).Determinant / det,
                +new Float2x2(A11, A12, A21, A22).Determinant / det
            );
        }

    }
}

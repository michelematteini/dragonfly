using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Math
{
    public struct Float2x3
    {
        #region Static Constructors

        /// <summary>
        /// Create a mapping from 2d coordinates to 3d points on a plane in 3d space, given the plane normal.
        /// </summary>
        /// <param name="planeNormal"></param>
        /// <returns></returns>
        public static Float2x3 PlanarMapping(Float3 planeNormal)
        {
            Float3 ay = Float3.UnitZ;
            Float3 ax = ay.Cross(planeNormal);
            if(ax.Length.IsAlmostZero())
            {
                ay = -Float3.UnitY;
                ax = ay.Cross(planeNormal);
            }
            ax = ax.Normal();
            ay = planeNormal.Cross(ax).Normal();
            return new Float2x3(ax, ay);
        }

        #endregion

        public float A11, A12, A13;
        public float A21, A22, A23;

        public Float2x3(
                float a11, float a12, float a13,
                float a21, float a22, float a23
            )
        {
            A11 = a11; A12 = a12; A13 = a13;
            A21 = a21; A22 = a22; A23 = a23;
        }

        public Float2x3(Float3 row1, Float3 row2) : this(row1.X, row1.Y, row1.Z, row2.X, row2.Y, row2.Z) { }

        //Float2 x Float2x3 non commutative

        public static Float3 operator *(Float2 v, Float2x3 m)
        {
            return new Float3(
                v.X * m.A11 + v.Y * m.A21,
                v.X * m.A12 + v.Y * m.A22,
                v.X * m.A13 + v.Y * m.A23
            );
        }

        //Float2x3 x Float3 non commutative

        public static Float2 operator *(Float2x3 m, Float3 v)
        {
            return new Float2(
                v.X * m.A11 + v.Y * m.A12 + v.Z * m.A13,
                v.X * m.A21 + v.Y * m.A22 + v.Z * m.A23
            );
        }

        // row / column access

        public Float2 GetColumn(int index)
        {
            switch (index % 3)
            {
                default:
                case 0: return new Float2(A11, A21);
                case 1: return new Float2(A12, A22);
                case 2: return new Float2(A13, A23);
            }
        }

        public Float3 GetRow(int index)
        {
            switch (index % 2)
            {
                default:
                case 0: return new Float3(A11, A12, A13);
                case 1: return new Float3(A21, A22, A23);
            }
        }

        public void SetColumn(int index, Float2 value)
        {
            switch (index % 3)
            {
                default:
                case 0:
                    A11 = value.X;
                    A21 = value.Y;
                    break;
                case 1:
                    A12 = value.X;
                    A22 = value.Y;
                    break;
                case 2:
                    A13 = value.X;
                    A23 = value.Y;
                    break;
            }
        }

        public void SetRow(int index, Float3 value)
        {
            switch (index % 2)
            {
                default:
                case 0:
                    A11 = value.X;
                    A12 = value.Y;
                    A13 = value.Z;
                    break;
                case 1:
                    A21 = value.X;
                    A22 = value.Y;
                    A23 = value.Z;
                    break;
            }
        }

        //Float2x3 x Float3x3 non commutative

        public static Float2x3 operator *(Float2x3 m1, Float3x3 m2)
        {
            return new Float2x3(
                //row 1
                m1.A11 * m2.A11 + m1.A12 * m2.A21 + m1.A13 * m2.A31,
                m1.A11 * m2.A12 + m1.A12 * m2.A22 + m1.A13 * m2.A32,
                m1.A11 * m2.A13 + m1.A12 * m2.A23 + m1.A13 * m2.A33,
                //row 2
                m1.A21 * m2.A11 + m1.A22 * m2.A21 + m1.A23 * m2.A31,
                m1.A21 * m2.A12 + m1.A22 * m2.A22 + m1.A23 * m2.A32,
                m1.A21 * m2.A13 + m1.A22 * m2.A23 + m1.A23 * m2.A33
            );
        }

        public static Float2x3 operator *(Float2x2 m1, Float2x3 m2)
        {
            return new Float2x3(
                //row 1
                m1.A11 * m2.A11 + m1.A12 * m2.A21,
                m1.A11 * m2.A12 + m1.A12 * m2.A22,
                m1.A11 * m2.A13 + m1.A12 * m2.A23,
                //row 2
                m1.A21 * m2.A11 + m1.A22 * m2.A21,
                m1.A21 * m2.A12 + m1.A22 * m2.A22,
                m1.A21 * m2.A13 + m1.A22 * m2.A23
            );
        }

    }
}

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dragonfly.Graphics.Math
{
    [StructLayout(LayoutKind.Explicit, Size = 64, CharSet = CharSet.Ansi)]
    public struct Float4x4 : IComparable
    {
		#region Static Constructors
		
        public static readonly Float4x4 Identity = new Float4x4(
                    1, 0, 0, 0,
                    0, 1, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1
                );

        public static Float4x4 Scale(float scale)
        {
            return Scale(scale, scale, scale);
        }

        public static Float4x4 Scale(Float3 scale)
        {
            return Scale(scale.X, scale.Y, scale.Z);
        }

        public static Float4x4 Scale(float sx, float sy, float sz)
		{
			return new Float4x4(
				sx, 0, 0, 0,
				0, sy, 0, 0,
				0, 0, sz, 0,
				0, 0, 0, 1
			);
		}

        public static Float4x4 Translation(Float3 t)
        {
            return Translation(t.X, t.Y, t.Z);
        }

        public static Float4x4 Translation(float tx, float ty, float tz)
		{
			return new Float4x4(
				1, 0, 0, 0,
				0, 1, 0, 0,
				0, 0, 1, 0,
				tx, ty, tz, 1
			);
		}
		
		public static Float4x4 RotationX(float radians)
		{
			float sin = FMath.Sin(radians);
            float cos = FMath.Cos(radians);
			return new Float4x4(
				1, 0, 0, 0,
				0, cos, sin, 0,
				0, -sin, cos, 0,
				0, 0, 0, 1
			);
		}
		
		public static Float4x4 RotationY(float radians)
		{
            float sin = FMath.Sin(radians);
            float cos = FMath.Cos(radians);
			return new Float4x4(
				cos,    0,      -sin,   0,
				0,      1,      0,      0,
				sin,    0,      cos,    0,
				0,      0,      0,      1
			);
		}
		
		public static Float4x4 RotationZ(float radians)
		{
            float sin = FMath.Sin(radians);
            float cos = FMath.Cos(radians);
			return new Float4x4(
				cos, sin, 0, 0,
				-sin, cos, 0, 0,
				0, 0, 1, 0,
				0, 0, 0, 1
			);
		}

        public static Float4x4 Rotation(Float3 eulerAngles)
        {
            return RotationX(eulerAngles.X) * RotationY(eulerAngles.Y) * RotationZ(eulerAngles.Z);
        }

        public static Float4x4 Rotation(Float3 axis, float radians)
        {
            float sin = FMath.Sin(radians);
            float cos = FMath.Cos(radians);
            float cos1 = 1 - cos;
            Float3 u = axis.Normal();

            return new Float4x4(
                cos + u.X * u.X * cos1, u.Y * u.X * cos1 + u.Z * sin, u.Z * u.X * cos1 - u.Y * sin, 0,
                u.X * u.Y * cos1 - u.Z * sin, cos + u.Y * u.Y * cos1, u.Z * u.Y * cos1 + u.X * sin, 0,
                u.X * u.Z * cos1 + u.Y * sin, u.Y * u.Z * cos1 - u.X * sin, cos + u.Z * u.Z * cos1, 0,
                0, 0, 0, 1
            );
        }

        public static Float4x4 Rotation(Float3 fromDirection, Float3 toDirection)
        {
            return Rotation(fromDirection, toDirection, Float3.NotParallelAxis(fromDirection, toDirection, Float3.UnitY));
        }

        public static Float4x4 Rotation(Float3 fromDirection, Float3 toDirection, Float3 upDirection)
        {
            // calculate "from" rotation basis
            Float3 fromNz = fromDirection.Normal();
            Float3 fromNx = upDirection.Cross(fromNz).Normal();
            Float3 fromNy = fromNz.Cross(fromNx);
            Float4x4 lookAtFrom = new Float4x4(fromNx, fromNy, fromNz).Transpose();

            // calculate "to" rotation basis
            Float3 toNz = toDirection.Normal();
            Float3 toNx = upDirection.Cross(toNz).Normal();
            Float3 toNy = toNz.Cross(toNx);
            Float4x4 rotateTo = new Float4x4(toNx, toNy, toNz);

            return lookAtFrom * rotateTo;
        }

        public static Float4x4 LookAt(Float3 position, Float3 direction, Float3 upDirection)
		{
			//calculate x, y, z axis (in view space)
			Float3 nz = direction.Normal();
			Float3 nx = upDirection.Cross(nz).Normal();
			Float3 ny = nz.Cross(nx);
			
			return new Float4x4(
				nx.X, ny.X, nz.X, 0,
				nx.Y, ny.Y, nz.Y, 0,
				nx.Z, ny.Z, nz.Z, 0,
				-nx.Dot(position), -ny.Dot(position), -nz.Dot(position), 1
			);
		}
		
		public static Float4x4 Perspective(float verticalFOV, float aspectRatio, float nearPlane, float farPlane)
		{
			float sy = 1.0f / (float)System.Math.Tan(verticalFOV / 2);
			float sx = sy / aspectRatio;
            float sz = 1.0f / (farPlane - nearPlane);

            if (!float.IsPositiveInfinity(farPlane))
            {
                return new Float4x4(
                    sx, 0, 0, 0,
                    0, sy, 0, 0,
                    0, 0, -nearPlane * sz, 1,
                    0, 0, nearPlane * farPlane * sz, 0
                );
            }
            else
            {
                return new Float4x4(
                    sx, 0, 0, 0,
                    0, sy, 0, 0,
                    0, 0, 0, 1,
                    0, 0, nearPlane, 0
                );
            }
		}

        public static Float4x4 Orthographic(float width, float height, float nearPlane, float farPlane)
        {
            float sz = 1.0f / (farPlane - nearPlane);

            return new Float4x4(
                2.0f / width, 0,              0,                     0,
                0,            2.0f / height,  0,                     0,
                0,            0,             -sz,                    0,
                0,            0,              1.0f + nearPlane * sz, 1
            );
        }

        #endregion

        [FieldOffset(00)] public float A11;
        [FieldOffset(04)] public float A12;
        [FieldOffset(08)] public float A13;
        [FieldOffset(12)] public float A14;

        [FieldOffset(16)] public float A21;
        [FieldOffset(20)] public float A22;
        [FieldOffset(24)] public float A23;
        [FieldOffset(28)] public float A24;

        [FieldOffset(32)] public float A31;
        [FieldOffset(36)] public float A32;
        [FieldOffset(40)] public float A33;
        [FieldOffset(44)] public float A34;

        [FieldOffset(48)] public float A41;
        [FieldOffset(52)] public float A42;
        [FieldOffset(56)] public float A43;
        [FieldOffset(60)] public float A44;

		public Float4x4(
				float a11, float a12, float a13, float a14,
				float a21, float a22, float a23, float a24,
				float a31, float a32, float a33, float a34,
				float a41, float a42, float a43, float a44
			)
		{
			A11 = a11; A12 = a12; A13 = a13; A14 = a14;
			A21 = a21; A22 = a22; A23 = a23; A24 = a24;
			A31 = a31; A32 = a32; A33 = a33; A34 = a34;
			A41 = a41; A42 = a42; A43 = a43; A44 = a44;
		}

        public Float4x4(Float4 row1, Float4 row2, Float4 row3, Float4 row4) 
            : this(row1.X, row1.Y, row1.Z, row1.W, 
                   row2.X, row2.Y, row2.Z, row2.W, 
                   row3.X, row3.Y, row3.Z, row3.W, 
                   row4.X, row4.Y, row4.Z, row4.W
                  )
        {
        }

        public Float4x4(Float3 row1, Float3 row2, Float3 row3)
            : this(row1.X, row1.Y, row1.Z, 0,
                   row2.X, row2.Y, row2.Z, 0,
                   row3.X, row3.Y, row3.Z, 0,
                   0, 0, 0, 1
                  )
        {
        }

        public Float4x4 Transpose()
        {
            return new Float4x4(
                A11, A21, A31, A41,
                A12, A22, A32, A42,
                A13, A23, A33, A43,
                A14, A24, A34, A44
            );
        }

        public float Determinant
        {
            get
            {
                Float3x3 subx = new Float3x3(A22, A23, A24, A32, A33, A34, A42, A43, A44);
                Float3x3 suby = new Float3x3(A21, A23, A24, A31, A33, A34, A41, A43, A44);
                Float3x3 subz = new Float3x3(A21, A22, A24, A31, A32, A34, A41, A42, A44);
                Float3x3 subw = new Float3x3(A21, A22, A23, A31, A32, A33, A41, A42, A43);
                return A11 * subx.Determinant - A12 * suby.Determinant + A13 * subz.Determinant - A14 * subw.Determinant;
            }
        }

        public float Trace
        {
            get
            {
                return A11 + A22 + A33 + A44;
            }
        }

        public Float3 Position
        {
            get
            {
                return new Float3(A41, A42, A43);
            }
            set
            {
                A41 = value.X;
                A42 = value.Y;
                A43 = value.Z;
            }
        }

        /// <summary>
        /// Returns the origin of the space defined by this matrix. Multiplying the result of this property to this matrix results in a zero vector.
        /// Equivalent to matrix.Invert().Position.
        /// </summary>
        public Float3 Origin
        {
            get
            {
                Float3 origin;
                origin.X = -(new Float3x3(A21, A22, A23, A31, A32, A33, A41, A42, A43).Determinant);
                origin.Y = +(new Float3x3(A11, A12, A13, A31, A32, A33, A41, A42, A43).Determinant);
                origin.Z = -(new Float3x3(A11, A12, A13, A21, A22, A23, A41, A42, A43).Determinant);
                return origin / Determinant;
            }
        }

        /// <summary>
        /// Given a screen resolution, returns the dimensions of a pixel in scree-space.
        /// <para/> This call should be used with a projection matrix.
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public Float2 PixelSizeAt(Int2 resolution)
        {
            return 2.0f / (resolution * new Float2(GetColumn(0).XYZ.Length, GetColumn(1).XYZ.Length));
        }

        public Float4x4 Invert()
        {
            // cofactor matrix
            Float4x4 c = new Float4x4();

            c.A11 += new Float3x3(A22, A23, A24, A32, A33, A34, A42, A43, A44).Determinant;
            c.A12 -= new Float3x3(A21, A23, A24, A31, A33, A34, A41, A43, A44).Determinant;
            c.A13 += new Float3x3(A21, A22, A24, A31, A32, A34, A41, A42, A44).Determinant;
            c.A14 -= new Float3x3(A21, A22, A23, A31, A32, A33, A41, A42, A43).Determinant;

            c.A21 -= new Float3x3(A12, A13, A14, A32, A33, A34, A42, A43, A44).Determinant;
            c.A22 += new Float3x3(A11, A13, A14, A31, A33, A34, A41, A43, A44).Determinant;
            c.A23 -= new Float3x3(A11, A12, A14, A31, A32, A34, A41, A42, A44).Determinant;
            c.A24 += new Float3x3(A11, A12, A13, A31, A32, A33, A41, A42, A43).Determinant;

            c.A31 += new Float3x3(A12, A13, A14, A22, A23, A24, A42, A43, A44).Determinant;
            c.A32 -= new Float3x3(A11, A13, A14, A21, A23, A24, A41, A43, A44).Determinant;
            c.A33 += new Float3x3(A11, A12, A14, A21, A22, A24, A41, A42, A44).Determinant;
            c.A34 -= new Float3x3(A11, A12, A13, A21, A22, A23, A41, A42, A43).Determinant;

            c.A41 -= new Float3x3(A12, A13, A14, A22, A23, A24, A32, A33, A34).Determinant;
            c.A42 += new Float3x3(A11, A13, A14, A21, A23, A24, A31, A33, A34).Determinant;
            c.A43 -= new Float3x3(A11, A12, A14, A21, A22, A24, A31, A32, A34).Determinant;
            c.A44 += new Float3x3(A11, A12, A13, A21, A22, A23, A31, A32, A33).Determinant;

            // compute inverse from cofactors
            return c.Transpose() / Determinant;
        }

        /// <summary>
        /// Return this matrix multiplied for the given translation. 
        /// Equivalent to creating a translation matrix first and then multiply, but faster.
        /// </summary>
        public Float4x4 Translate(Float3 t)
        {
            Float4x4 result = this;
            result.A11 += A14 * t.X;
            result.A12 += A14 * t.Y;
            result.A13 += A14 * t.Z;
            result.A21 += A24 * t.X;
            result.A22 += A24 * t.Y;
            result.A23 += A24 * t.Z;
            result.A31 += A34 * t.X;
            result.A32 += A34 * t.Y;
            result.A33 += A34 * t.Z;
            result.A41 += A44 * t.X;
            result.A42 += A44 * t.Y;
            result.A43 += A44 * t.Z;
            return result;
        }

        /// <summary>
        /// When applied on a rotation matrix, returns the three euler angles
        /// </summary>
        /// <returns></returns>
        public Float3 GetRotationAngles()
        {
            Float3 rotation = new Float3(
                (float)System.Math.Atan2(A23, A33),
                (float)System.Math.Atan2(-A13, new Float2(A23, A33).Length),
                (float)System.Math.Atan2(A12, A11)
            );
            return rotation;
        }

        public Float4 GetColumn(int index)
        {
            switch(index % 4)
            {
                default: case 0: return new Float4(A11, A21, A31, A41);
                case 1: return new Float4(A12, A22, A32, A42);
                case 2: return new Float4(A13, A23, A33, A43);
                case 3: return new Float4(A14, A24, A34, A44);
            }
        }

        public Float4 GetRow(int index)
        {
            switch (index % 4)
            {
                default: case 0: return new Float4(A11, A12, A13, A14);
                case 1: return new Float4(A21, A22, A23, A24);
                case 2: return new Float4(A31, A32, A33, A34);
                case 3: return new Float4(A41, A42, A43, A44);
            }
        }

        /// <summary>
        /// Return the normalized result of transforming the vector pointing in the X direction with this matrix.
        /// </summary>
        public Float3 GetXAxis()
        {
            return ((GetRow(0) + GetRow(3)).ToFloat3() - GetRow(3).ToFloat3()).Normal();
        }

        /// <summary>
        /// Return the normalized result of transforming the vector pointing in the Y direction with this matrix.
        /// </summary>
        public Float3 GetYAxis()
        {
            return ((GetRow(1) + GetRow(3)).ToFloat3() - GetRow(3).ToFloat3()).Normal();
        }

        /// <summary>
        /// Return the normalized result of transforming the vector pointing in the Z direction with this matrix.
        /// </summary>
        public Float3 GetZAxis()
        {
            return ((GetRow(2) + GetRow(3)).ToFloat3() - GetRow(3).ToFloat3()).Normal();
        }

        /// <summary>
        /// Return the normalized direction that transformed with this matrix points in the X direction.
        /// </summary>
        public Float3 GetXDirection()
        {
            return GetColumn(0).XYZ.Normal();
        }

        /// <summary>
        /// Return the normalized direction that transformed with this matrix points in the Y direction.
        /// </summary>
        public Float3 GetYDirection()
        {
            return GetColumn(1).XYZ.Normal();
        }

        /// <summary>
        /// Return the normalized direction that transformed with this matrix points in the Z direction.
        /// </summary>
        public Float3 GetZDirection()
        {
            return GetColumn(2).XYZ.Normal();
        }

        public float[] ToArray()
        {
            return new float[] { A11, A21, A31, A41, A12, A22, A32, A42, A13, A23, A33, A43, A14, A24, A34, A44 };
        }

        public void CopyTo(float[] destArray, int startIndex)
        {
            destArray[startIndex++] = A11;
            destArray[startIndex++] = A21;
            destArray[startIndex++] = A31;
            destArray[startIndex++] = A41;

            destArray[startIndex++] = A12;
            destArray[startIndex++] = A22;
            destArray[startIndex++] = A32;
            destArray[startIndex++] = A42;

            destArray[startIndex++] = A13;
            destArray[startIndex++] = A23;
            destArray[startIndex++] = A33;
            destArray[startIndex++] = A43;

            destArray[startIndex++] = A14;
            destArray[startIndex++] = A24;
            destArray[startIndex++] = A34;
            destArray[startIndex] = A44;
        }

        #region Operators

        //Float2 x Float4x4 non commutative

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2 operator *(Float2 v, Float4x4 m)
        {
            Float4 res = new Float4(v.X, v.Y, 0, 1) * m;
            return res.XY / res.W;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2 operator *(Float4x4 m, Float2 v)
        {
            Float4 res = m * new Float4(v.X, v.Y, 0, 1);
            return res.XY / res.W;
        }

        //Float3 x Float4x4 non commutative

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator *(Float3 v, Float4x4 m)
		{
            Float4 res = new Float4(v, 1) * m;
			return res.XYZ / res.W;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator *(Float4x4 m, Float3 v)
        {
            Float4 res = m * new Float4(v, 1);
            return res.XYZ / res.W;
        }

        //Float4 x Float4x4 non commutative

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4 operator *(Float4 v, Float4x4 m)
		{
			return new Float4 (
                v.X * m.A11 + v.Y * m.A21 + v.Z * m.A31 + v.W * m.A41,
                v.X * m.A12 + v.Y * m.A22 + v.Z * m.A32 + v.W * m.A42,
                v.X * m.A13 + v.Y * m.A23 + v.Z * m.A33 + v.W * m.A43,
                v.X * m.A14 + v.Y * m.A24 + v.Z * m.A34 + v.W * m.A44
			);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4 operator *(Float4x4 m, Float4 v)
        {
            return new Float4(
                v.X * m.A11 + v.Y * m.A12 + v.Z * m.A13 + v.W * m.A14,
                v.X * m.A21 + v.Y * m.A22 + v.Z * m.A23 + v.W * m.A24,
                v.X * m.A31 + v.Y * m.A32 + v.Z * m.A33 + v.W * m.A34,
                v.X * m.A41 + v.Y * m.A42 + v.Z * m.A43 + v.W * m.A44
            );
        }

        //Float4x4 x Float4x4 non commutative

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4x4 operator +(Float4x4 m1, Float4x4 m2)
		{
			return new Float4x4(
				m1.A11 + m2.A11, m1.A12 + m2.A12, m1.A13 + m2.A13, m1.A14 + m2.A14,
				m1.A21 + m2.A21, m1.A22 + m2.A22, m1.A23 + m2.A23, m1.A24 + m2.A24,
				m1.A31 + m2.A31, m1.A32 + m2.A32, m1.A33 + m2.A33, m1.A34 + m2.A34,
				m1.A41 + m2.A41, m1.A42 + m2.A42, m1.A43 + m2.A43, m1.A44 + m2.A44
			);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4x4 operator -(Float4x4 m1, Float4x4 m2)
        {
            return m1 + (m2 * -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4x4 operator *(Float4x4 m1, Float4x4 m2)
		{
			return new Float4x4(
				//row 1
				m1.A11 * m2.A11 + m1.A12 * m2.A21 + m1.A13 * m2.A31 + m1.A14 * m2.A41,
				m1.A11 * m2.A12 + m1.A12 * m2.A22 + m1.A13 * m2.A32 + m1.A14 * m2.A42,
				m1.A11 * m2.A13 + m1.A12 * m2.A23 + m1.A13 * m2.A33 + m1.A14 * m2.A43,
				m1.A11 * m2.A14 + m1.A12 * m2.A24 + m1.A13 * m2.A34 + m1.A14 * m2.A44,
				//row 2
				m1.A21 * m2.A11 + m1.A22 * m2.A21 + m1.A23 * m2.A31 + m1.A24 * m2.A41,
				m1.A21 * m2.A12 + m1.A22 * m2.A22 + m1.A23 * m2.A32 + m1.A24 * m2.A42,
				m1.A21 * m2.A13 + m1.A22 * m2.A23 + m1.A23 * m2.A33 + m1.A24 * m2.A43,
				m1.A21 * m2.A14 + m1.A22 * m2.A24 + m1.A23 * m2.A34 + m1.A24 * m2.A44,
				//row 3
				m1.A31 * m2.A11 + m1.A32 * m2.A21 + m1.A33 * m2.A31 + m1.A34 * m2.A41,
				m1.A31 * m2.A12 + m1.A32 * m2.A22 + m1.A33 * m2.A32 + m1.A34 * m2.A42,
				m1.A31 * m2.A13 + m1.A32 * m2.A23 + m1.A33 * m2.A33 + m1.A34 * m2.A43,
				m1.A31 * m2.A14 + m1.A32 * m2.A24 + m1.A33 * m2.A34 + m1.A34 * m2.A44,
				//row 4
				m1.A41 * m2.A11 + m1.A42 * m2.A21 + m1.A43 * m2.A31 + m1.A44 * m2.A41,
				m1.A41 * m2.A12 + m1.A42 * m2.A22 + m1.A43 * m2.A32 + m1.A44 * m2.A42,
				m1.A41 * m2.A13 + m1.A42 * m2.A23 + m1.A43 * m2.A33 + m1.A44 * m2.A43,
				m1.A41 * m2.A14 + m1.A42 * m2.A24 + m1.A43 * m2.A34 + m1.A44 * m2.A44
			);
		}

        //Float4x4 x float

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4x4 operator *(Float4x4 m, float k)
        {
            return new Float4x4(
                m.A11 * k, m.A12 * k, m.A13 * k, m.A14 * k,
                m.A21 * k, m.A22 * k, m.A23 * k, m.A24 * k,
                m.A31 * k, m.A32 * k, m.A33 * k, m.A34 * k,
                m.A41 * k, m.A42 * k, m.A43 * k, m.A44 * k
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4x4 operator *(float k, Float4x4 m)
        {
            return m * k;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float4x4 operator / (Float4x4 m, float k)
        {
            return m * (1 / k);
        }

        #endregion

        public bool IsIdentity()
        {
            return A11 == 1.0f && A22 == 1.0f && A33 == 1.0f && A44 == 1.0f
                && A12 == 0.0f && A13 == 0.0f && A14 == 0.0f
                && A21 == 0.0f && A23 == 0.0f && A24 == 0.0f
                && A31 == 0.0f && A32 == 0.0f && A34 == 0.0f
                && A41 == 0.0f && A42 == 0.0f && A43 == 0.0f;
        }

        public override string ToString()
        {
			string rowSep = ", ";
			string ff = "0.000";
            return "" +
				String.Format("[{0}, {1}, {2}, {3}]{4}", A11.ToString(ff), A12.ToString(ff), A13.ToString(ff), A14.ToString(ff), rowSep) +
				String.Format("[{0}, {1}, {2}, {3}]{4}", A21.ToString(ff), A22.ToString(ff), A23.ToString(ff), A24.ToString(ff), rowSep) +
				String.Format("[{0}, {1}, {2}, {3}]{4}", A31.ToString(ff), A32.ToString(ff), A33.ToString(ff), A34.ToString(ff), rowSep) +
				String.Format("[{0}, {1}, {2}, {3}]", A41.ToString(ff), A42.ToString(ff), A43.ToString(ff), A44.ToString(ff));
        }

        public int CompareTo(object obj)
        {
            //here only for compatibility with constraints on generics
            return 0;
        }
    }
}

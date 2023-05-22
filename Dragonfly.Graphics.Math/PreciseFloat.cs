using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Math
{
    /// <summary>
    /// Helper class that manage operations on a single floating point number, storing errors on calculations along its results.
    /// All operators are carried out in double precision.
	/// This encoding can be then propagated to shaders as a float2, where the sum of its component give the actual value, 
	/// but sum and subtractions can be carried out on a per-component basis allowing high precision value and differentiation even on very large numbers.
    /// </summary>
    public struct PreciseFloat
    {
		public static readonly PreciseFloat Zero = new PreciseFloat(0);
		public static readonly PreciseFloat Infinity = new PreciseFloat() { FloatValue = float.PositiveInfinity };

		public float FloatValue, FloatError;

		public double Value
		{ 
			get { return (double)FloatValue + FloatError; } 
		}

		public Float2 ToFloat2()
        {
			return new Float2(FloatValue, FloatError);
        }

		public static implicit operator double(PreciseFloat value)
		{
			return value.Value;
		}

		public PreciseFloat(double value)
        {
			FloatValue = (float)value;
			FloatError = (float)(value - FloatValue);
        }

		public static PreciseFloat operator +(PreciseFloat v1, PreciseFloat v2)
		{
			return new PreciseFloat(v1.Value + v2.Value);
		}

		public static PreciseFloat operator -(PreciseFloat v1, PreciseFloat v2)
		{
			return new PreciseFloat(v1.Value - v2.Value);
		}

		public static PreciseFloat operator +(PreciseFloat v1, float v2)
		{
			return new PreciseFloat(v1.Value + v2);
		}

		public static PreciseFloat operator -(PreciseFloat v1, float v2)
		{
			return new PreciseFloat(v1.Value - v2);
		}

		public static PreciseFloat operator -(float v1, PreciseFloat v2)
		{
			return new PreciseFloat(v1 - v2.Value);
		}

		public static PreciseFloat operator *(PreciseFloat v1, PreciseFloat v2)
		{
			return new PreciseFloat(v1.Value * v2.Value);
		}

		public static PreciseFloat operator *(PreciseFloat v1, float v2)
		{
			return new PreciseFloat(v1.Value * v2);
		}

		public static PreciseFloat operator *(float v1, PreciseFloat v2)
		{
			return new PreciseFloat(v1 * v2.Value);
		}

		public static PreciseFloat operator /(PreciseFloat v1, PreciseFloat v2)
		{
			return new PreciseFloat(v1.Value / v2.Value);

		}

        public override bool Equals(object obj)
        {
			if (obj is PreciseFloat other)
			{
				return this == other;
			}
			return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(PreciseFloat v1, PreciseFloat v2)
		{
			return v1.FloatValue == v2.FloatValue && v1.FloatError == v2.FloatError;
		}

		public static bool operator !=(PreciseFloat v1, PreciseFloat v2)
		{
			return !(v1 == v2);
		}

		public override string ToString()
        {
			return Value.ToString();
        }

    }
}

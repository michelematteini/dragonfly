using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.Math
{
    public class FRandom
    {
        private Random rnd;
        private Stack<int> seeds;

        public FRandom()
        {
            rnd = new Random();
        }

        public FRandom(int seed)
        {
            rnd = new Random(seed);
        }

        public void PushSequence()
        {
            if (seeds == null)
                seeds = new Stack<int>();

            seeds.Push(rnd.Next());
            rnd = new Random(seeds.Peek());
        }

        public void PopSequence()
        {
            if (seeds == null || seeds.Count == 0)
                return;

            rnd = new Random(seeds.Pop());
        }

        public float NextFloat()
        {
            return (float)rnd.NextDouble();
        }

        public float NextFloat(float min, float max)
        {
            return (float)rnd.NextDouble() * (max - min) + min;
        }

        public bool NextBool()
        {
            return rnd.NextDouble() > 0.5;
        }

        public float NextSign()
        {
            return rnd.NextDouble() > 0.5 ? 1.0f : -1.0f;
        }

        public float NextSignedFloat()
        {
            return (float)rnd.NextDouble() * NextSign();
        }      

        public int NextInt()
        {
            return rnd.Next();
        }
        public int NextInt(int maxValue)
        {
            return rnd.Next(maxValue);
        }

        public Float2 NextNorm2()
        {
            float radians = NextFloat() * FMath.TWO_PI;
            return new Float2((float)System.Math.Cos(radians), (float)System.Math.Sin(radians));
        }

        public Float3 NextNorm3()
        {
            return new Float3(NextFloat() - 0.5f, NextFloat() - 0.5f, NextFloat() - 0.5f).Normal();
        }

        public Float2 NextFloat2(Float2 min, Float2 max)
        {
            Float2 delta = max - min;
            Float2 rndOffset = new Float2(NextFloat(), NextFloat()) * delta;
            return min + rndOffset;
        }

        public Float3 NextFloat3(Float3 min, Float3 max)
        {
            Float3 delta = max - min;
            Float3 rndOffset = new Float3(NextFloat(), NextFloat(), NextFloat()) * delta;
            return min + rndOffset;
        }

        public Float3 NextSatColor()
        {
            float hue = NextFloat() * 6;
            float frac = hue - (int)hue;
            Float3 color = new Float3();
            color.R = hue < 2 ? 1.0f : (hue < 3 ? 1 - frac : (hue < 5 ? 0 : frac));
            color.G = hue < 1 ? 0 : (hue < 2 ? frac : (hue < 4 ? 1 : (hue < 5 ? 1 - frac : 0)));
            color.B = hue < 3 ? 0 : (hue < 4 ? frac : 1);
            return color;
        }

        public Float3 PerturbateNormal(Float3 normal, float minAngle, float maxAngle)
        {
            return normal * Float4x4.Rotation(NextFloat3((Float3)minAngle, (Float3)maxAngle) * NextSign());
        }

        public List<Float3> RandomWalk(Float3 start, Float3 end, int pointsCount, float maxDistanceFromPath, float minDistanceBetweenPoints)
        {
            float step = 1.0f / (pointsCount + 1);
            float maxStepShift = (step - minDistanceBetweenPoints / (end - start).Length) * 0.5f;
            Float3 pathNormal = (end - start).Normal();

            List<Float3> path = new List<Float3>();
            path.Add(start);
            
            for (int i = 1; i <= pointsCount; i++)
            {
                float splitLocation = step * i + NextSignedFloat() * maxStepShift;
                Float3 splitPnt = start.Lerp(end, splitLocation);

                Float3 rndDisplace = NextNorm3();
                rndDisplace = maxDistanceFromPath * (rndDisplace - rndDisplace.ProjectTo(pathNormal));

                path.Add(splitPnt + rndDisplace);
            }

            path.Add(end);
            return path;
        }

    }
}

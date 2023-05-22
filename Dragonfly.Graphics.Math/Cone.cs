
namespace Dragonfly.Graphics.Math
{
    public struct Cone
    {
        public Float3 Top, BaseCenter;
        public float Radius;

        public Cone(Float3 top, Float3 baseCenter, float radius)
        {
            Top = top;
            BaseCenter = baseCenter;
            Radius = radius;
        }

        public AABox ToBoundingBox()
        {
            Float3 hDir = (BaseCenter - Top).Normal();
            Float3 bbDelta = Radius * new Float3(
                hDir.YZ.Length.Saturate(),
                hDir.XZ.Length.Saturate(),
                hDir.XY.Length.Saturate()
            );
            return new AABox(BaseCenter - bbDelta, BaseCenter + bbDelta).Add(Top);
        }

    }
}

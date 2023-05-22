using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A spherical collider for the shadow cameras.
    /// Avoids shadow cameras being positioned inside the specified region.
    /// The shadow atlas will search any component with return type IComponent(ShadowCameraCollider)
    /// </summary>
    public struct ShadowCameraCollider
    {
        public TiledFloat3 Center;
        public float Radius;

        public Sphere ToSphere(Int3 referenceTile)
        {
            return new Sphere(Center.ToFloat3(referenceTile), Radius);
        }
    }

}

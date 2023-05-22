using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public class CompLightDirectional : CompLight
    {
        public CompLightDirectional(Component owner, Float3 color, float intensity) : base(owner, color, intensity)
        {
            AvgDistanceMeters = 15.0E10f; // default to sun to earth distance
            RadiusMeters = new CompValue<float>(this, 7.0E8f); // default to sun radius
        }
        public CompLightDirectional(Component owner, Float3 direction, Float3 color) : this(owner, color, 1.0f) { }


        public override bool HasPosition { get { return false; } }

        /// <summary>
        /// Set the distance of this light from the scene. 
        /// For a truly directional light this distance should be infinitely large, but a finite value can be set to achieve a softer lighing.
        /// </summary>
        public float AvgDistanceMeters { get; set; }

        /// <summary>
        /// The size of the light source in meters. This is taken into account  if the AverageDistanceMeters is finite.
        /// </summary>
        public CompValue<float> RadiusMeters { get; private set; }
    }
}

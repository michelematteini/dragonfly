using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Represents a geometric surface as a set of vertices and indices, with the associated graphic resources, that can be used to initialize a CompMesh drawable.
    /// </summary>
    public interface IMeshGeometry
    {
        /// <summary>
        /// The 3d bounding box of the current geometry.
        /// </summary>
        AABox BoundingBox { get; }

        /// <summary>
        /// Returns true if graphic resources for rendering this geometry are ready.
        /// </summary>
        bool Available { get; }

        VertexBuffer VertexBuffer { get; }

        IndexBuffer IndexBuffer { get; }
    }
}

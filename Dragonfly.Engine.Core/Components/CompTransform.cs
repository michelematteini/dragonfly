using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.Engine.Core
{

    public abstract class CompTransform : Component<TiledFloat4x4>
    {
        public const float WorldTileSize = 1024.0f;

        static CompTransform()
        {
            TiledFloat.TileSize = WorldTileSize;
        }


        public CompTransform(Component owner) : base(owner)
        {

        }

        protected sealed override TiledFloat4x4 getValue()
        {
            TiledFloat4x4 transform = GetLocalTransform();
            CompTransform parentTransformComp = GetFirstAncestor<CompTransform>();
            if (parentTransformComp != null)
                transform *= parentTransformComp.GetValue();
            
            return transform;
        }

        /// <summary>
        /// Returns the transform applied by this transformation component only, without taking parent transforms into account.
        /// The world tile return by this function is only taken into account if this it the top-most transformation component.
        /// </summary>
        public abstract TiledFloat4x4 GetLocalTransform();

    }
}


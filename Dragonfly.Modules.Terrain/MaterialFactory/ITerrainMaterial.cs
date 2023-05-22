using Dragonfly.Engine.Core;
using System;
using System.Collections.Generic;

namespace Dragonfly.Terrain
{
    public interface ITerrainMaterial
    {
        void OnLODChanged(CompTerrainTile compTerrainTile);
    }
}

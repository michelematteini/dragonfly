 using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    internal class CompLightTableManager : Component, ICompUpdatable
    {
        private LightTable lightTable;

        internal CompLightTableManager(Component parent) : base(parent)
        {
            lightTable = new LightTable(this);
        }

        internal CompCamera GetReferenceCamera()
        {
            return Context.GetModule<BaseMod>().MainPass.Camera;
        }

        public UpdateType NeededUpdates => UpdateType.FrameStart2;

        public void Update(UpdateType updateType)
        {
            // skip lights update if no reference camera that display them is available
            if (GetReferenceCamera() == null) return;

            // pre-fill light table with shadowmap data
            CompShadowAtlas shadows = GetComponent<CompShadowAtlas>();
            shadows.Update();
            shadows.FillShadowmapTable(lightTable);

            // query for supported lights
            IReadOnlyList<CompLightPoint> pointLightList = GetComponents<CompLightPoint>();
            IReadOnlyList<CompLightSpot> spotLightList = GetComponents<CompLightSpot>();
            IReadOnlyList<CompLightDirectional> dirLightList = GetComponents<CompLightDirectional>();


            IVolume cameraVolume = GetReferenceCamera().Volume;
            lightTable.Reset();

            // fill directional light parameters
            for (int i = 0; i < dirLightList.Count; i++)
            {
                lightTable.AddLightData(dirLightList[i], shadows.GetShadowmapsCount(dirLightList[i]), shadows.GetFirstShadowmapIndex(dirLightList[i]));
            }

            // fill point light parameters
            for (int i = 0; i < pointLightList.Count; i++)
            {
                if (!cameraVolume.Intersects(pointLightList[i].GetBoundingBox()))
                    continue;

                lightTable.AddLightData(pointLightList[i], shadows.GetShadowmapsCount(pointLightList[i]), shadows.GetFirstShadowmapIndex(pointLightList[i]));
            }

            // fill spot light parameters
            for (int i = 0; i < spotLightList.Count; i++)
            {
                if (!cameraVolume.Intersects(spotLightList[i].GetBoundingBox()))
                    continue;
                lightTable.AddLightData(spotLightList[i], shadows.GetShadowmapsCount(spotLightList[i]), shadows.GetFirstShadowmapIndex(spotLightList[i]));
            }

            lightTable.UploadValues();

            // update shaders
            Context.Scene.Globals.SetParam("lightCount", lightTable.LightCount);
            Context.Scene.Globals.SetParam("lightList", lightTable.Buffer);
        }
    }
}

using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    internal class ShadowState
    {
        private TextureAtlas parentAtlas;
        private bool queuedForRender;

        public ShadowState(CompLight parentLight, TextureAtlas parentAtlas, int viewCount)
        {
            ShadowMaps = new SubTextureReference[viewCount];
            CameraList = new CompCamera[viewCount];
            CameraTransforms = new CompTransformStack[viewCount];
            this.parentAtlas = parentAtlas;
            LightTableIndex = -1;
            ParentLight = parentLight;
        }

        public CompCamera[] CameraList { get; private set; }

        public CompTransformStack[] CameraTransforms { get; private set; }

        public SubTextureReference[] ShadowMaps { get; private set; }

        public int Resolution
        {
            get{ return ShadowMaps.Length > 0 ? ShadowMaps[0].Resolution.Width : 0; }
        }

        public Float3 Position
        {
            get { return CameraList[0].LocalPosition; }
        }

        public CompLight ParentLight { get; private set; }

        /// <summary>
        /// If true, the shadow is no longer updated
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Index of the first shadowmap of this state in the light table.
        /// </summary>
        public int LightTableIndex { get; set; }

        /// <summary>
        /// If true, the shadow is available on the atlas
        /// </summary>
        public bool Rendered { get; set; }

        public bool QueuedForRender
        {
            get { return queuedForRender; }

            set
            {
                if (value == queuedForRender) return;

                if(value)
                {
                    parentAtlas.Pass.CameraList.AddRange(CameraList);
                }
                else
                {
                    foreach (CompCamera shadowCam in CameraList)
                        parentAtlas.Pass.CameraList.Remove(shadowCam);
                }
                queuedForRender = value;
            }
        }

        public bool TryAllocShadowMaps(int resolution)
        {
            List<SubTextureReference> newSM = parentAtlas.Layout.TryAllocateSubTextureArray((Int2)resolution, ShadowMaps.Length);
            if (newSM.Count == 0) return false;

            newSM.CopyTo(ShadowMaps);
            return true;
        }

        public void Delete()
        {
            QueuedForRender = false; // removes cameras from view
            ReleaseShadowMaps();

            // release all components
            for (int i = 0; i < CameraList.Length; i++)
                CameraList[i].Dispose();
        }

        public void ReleaseShadowMaps()
        {
            for (int i = 0; i < ShadowMaps.Length; i++)
                ShadowMaps[i].Release();
        }
    }
}

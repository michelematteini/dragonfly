using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    public class CompMeshPicker : Component<CompMesh>, ICompUpdatable
    {
        public enum Mode
        {
            Disabled = 0,
            PickOnce,
            RealTime
        }

        private Byte4[] selectionCache;
        private Int2 selectionRes;
        private bool frameRendered;

        internal CompMeshPicker(Component owner) : base(owner)
        {           
            CompRenderBuffer meshPickingBuffer = new CompRenderBuffer(this, SurfaceFormat.Color, RenderBufferResizeStyle.BackbufferOver4);
            Pass = new CompRenderPass(this, Context.GetModule<BaseMod>().Settings.MaterialClasses.Solid, meshPickingBuffer);
            Pass.MainClass = "PickingPass";
            Pass.Active = false; // disabled by default, if picking is not needed, would just require an extra pass to be rendered
            PickMode = Mode.Disabled; // disabled by default
            frameRendered = false;
        }

        public UpdateType NeededUpdates 
        { 
            get 
            { 
                return ((PickMode != Mode.Disabled || frameRendered) && !Pass.RenderBuffer.LoadingRequired) ? UpdateType.FrameStart1 : UpdateType.None; 
            } 
        }

        public CompRenderPass Pass { get; private set; }

        public Mode PickMode { get; set; }

        public void Update(UpdateType updateType)
        {
            if(!frameRendered)
            {
                // start rendering
                Pass.Active = true;
                frameRendered = true;
            }
            else
            {
                // stop rendering
                Pass.Active = false;

                // update selection cache if size changed
                if (selectionCache == null || (Pass.RenderBuffer[0].Width * Pass.RenderBuffer[0].Height) != selectionCache.Length)
                {
                    selectionCache = new Byte4[Pass.RenderBuffer[0].Width * Pass.RenderBuffer[0].Height];
                    selectionRes = Pass.RenderBuffer[0].Resolution;
                }

                // try to retrieve the rendered data
                if(Pass.RenderBuffer[0].TryGetSnapshotData<Byte4>(selectionCache))
                {
                    // prepare state for the next picking
                    frameRendered = false;
                    if (PickMode == Mode.PickOnce)
                        PickMode = Mode.Disabled;
                }
            }          
        }

        protected override CompMesh getValue()
        {
            if (selectionCache == null)
                return null;

            // retrieve hovered mesh ID
            Int2 hoveredPixelLoc = (Int2)Float2.Max((Float2)selectionRes - 1, Context.Input.GetDevice<Mouse>().Position.XY * selectionRes);
            Byte4 hoveredPix = selectionCache[hoveredPixelLoc.X + hoveredPixelLoc.Y * selectionRes.Width];
            hoveredPix.A = 0;
            int MeshID = hoveredPix.ToInt();

            // search for the mesh name and return it
            foreach (CompMesh m in GetComponents<CompDrawable>())
            {
                if (m.ID != MeshID)
                    continue;

                return m; // selected mesh found!
            }

            return null; // not found ? this point should not be reached
        }
    }
}

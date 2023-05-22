using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Contains the logic that converts UI a windows or control z-index to a render order value to be assigned to its material.
    /// </summary>
    internal static class UiZIndex
    {
        public const long MaxWindowCount = 512;
        public const long MaxZIndex = 1024;

        /// <summary>
        /// Returns the maximum value that can be used as render order from the UI, which results in the top-most element.
        /// </summary>
        public static long TopRenderOrder
        {
            get
            {
                return (1/*non windowed controls*/ + MaxWindowCount) * GetWndRenderOrderCount();
            }
        }

        public static long BottomRenderOrder
        {
            get
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Returns the top-most render order for the window with the specified z-index.
        /// </summary>
        private static long GetWndTopRenderOrder(uint wndZIndex)
        {
            return TopRenderOrder - wndZIndex * GetWndRenderOrderCount();
        }

        /// <summary>
        /// Returns the number of different render order values allocated per window.
        /// </summary>
        private static long GetWndRenderOrderCount()
        {
            return 1/*skin*/ + (MaxZIndex + 1)/*custom zindexed layers*/ + 1/*text*/;
        }

        /// <summary>
        /// Returns the priority of the skin mesh in a window with the give zIndex
        /// </summary>
        public static long ToWndSkinRenderOrder(uint wndZIndex)
        {
            return GetWndTopRenderOrder(wndZIndex) - 1 /*below text*/ - (MaxZIndex + 1) /*below custom materials*/;
        }

        /// <summary>
        /// Returns the priority of the custom ui meshes in a window with the give zIndex
        /// </summary>
        public static long ToCustomMeshRenderOrder(uint wndZIndex, uint customMeshZIndex)
        {
            return GetWndTopRenderOrder(wndZIndex) - 1 /*below text*/ - customMeshZIndex;
        }

        /// <summary>
        /// Returns the priority of the sprite text in a window with the give zIndex
        /// </summary>
        public static long ToTextRenderOrder(uint wndZIndex)
        {
            return GetWndTopRenderOrder(wndZIndex);
        }

    }
}

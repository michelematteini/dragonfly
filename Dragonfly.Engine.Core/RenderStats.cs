namespace Dragonfly.Engine.Core
{
    public struct RenderStats
    {
        /// <summary>
        /// Number of rendered polygons
        /// </summary>
        public int PolygonCount;
        /// <summary>
        /// Number of submitted draw calls
        /// </summary>
        public int DrawCallCount;
        /// <summary>
        /// Number of processed drawables.
        /// </summary>
        public int ProcessedDrawableCount;

        public static RenderStats operator +(RenderStats s1, RenderStats s2)
        {
            s1.DrawCallCount += s2.DrawCallCount;
            s1.PolygonCount += s2.PolygonCount;
            s1.ProcessedDrawableCount += s2.ProcessedDrawableCount;
            return s1;
        }
    }
}

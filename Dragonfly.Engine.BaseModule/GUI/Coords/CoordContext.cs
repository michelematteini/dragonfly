using System;
using System.Collections.Generic;
using System.Threading;

namespace Dragonfly.BaseModule
{
    public class CoordContext
    {
        public CoordContext(IUiCanvas canvas, int fontSize)
        {
            Canvas = canvas;
            FontSize = fontSize;
        }

        /// <summary>
        /// The global font scaling factor in pixel.
        /// </summary>
        public int FontSize { get; set; }

        public IUiCanvas Canvas { get; private set; }

        public float ScreenAspectRatio
        {
            get
            {
                return (float)Canvas.PixelSize.Width / Canvas.PixelSize.Height;
            }
        }

        #region Local Context stacks

        private static ThreadLocal<Stack<CoordContext>> LocalContexts = new ThreadLocal<Stack<CoordContext>>(() => new Stack<CoordContext>(), false);

        public static void Push(CoordContext context)
        {
            LocalContexts.Value.Push(context);
        }

        public static void Pop()
        {
            LocalContexts.Value.Pop();
        }

        public static CoordContext Current
        {
            get
            {
                return LocalContexts.Value.Peek();
            }
        }

        #endregion
    }


}

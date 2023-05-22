using DragonflyGraphicsWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.API.Common
{
    /// <summary>
    /// A thread-safe list of native resources resources to be released, that have to wait to not be used anymore by in-flight frames.
    /// </summary>
    internal class FrameDeferredReleaseList
    {
        private List<List<DF_Resource>> toBeReleased;
        private object releaseLock;
        private int curFrameIndex;
        private int delayLeft;

        public FrameDeferredReleaseList(int swapChainFrameCount)
        {
            curFrameIndex = -1; // a negative value, meaning no frame has been executed
            toBeReleased = new List<List<DF_Resource>>();
            for (int i = 0; i < swapChainFrameCount; i++)
                toBeReleased.Add(new List<DF_Resource>());
            releaseLock = new object();
        }

        public void NewFrame(int frameIndex)
        {
            if(delayLeft > 0)
            {
                // wait additional frames before releasing
                delayLeft--;
                return; 
            }

            if (curFrameIndex >= 0)
            {
                // release all the resources released in the previous frame with the same index
                for (int i = 0; i < toBeReleased[frameIndex].Count; i++)
                    toBeReleased[frameIndex][i].Release();
                toBeReleased[frameIndex].Clear();
            }

            curFrameIndex = frameIndex;
        }


        public void DeferredRelease(DF_Resource resource)
        {
            lock (releaseLock)
            {
                toBeReleased[System.Math.Max(0, curFrameIndex)].Add(resource);
            }
        }

        /// <summary>
        /// Delay all the releases by a number of frame equal to the swapchain buffer count.
        /// </summary>
        public void DelayAll(int delayFrames)
        {
            delayLeft = delayFrames;
        }
    }
}

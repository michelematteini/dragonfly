using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Dragonfly.Engine.Core
{
	public class Timeline
	{
        private const int MAX_FPS = 3000; // max fps reported by this class
        private const int SMOOTHING_FRAME_COUNT = 4; // number of frame times used to predict a smooth delta time for the current frame

        private DateTime startDate;
		private long lastUpdateTicks;
        private List<float> frameDurationHistory;

        internal Timeline(DateTime startDate)
		{
			this.startDate = startDate;
			TimeFlowRate = 0;
            SecondsFromStart = new PreciseFloat(0);
            RealSecondsFromStart = new PreciseFloat(0);
            frameDurationHistory = new List<float>(SMOOTHING_FRAME_COUNT);
		}
		
		public float LastFrameDuration	{ get; private set; }	

        public float RealFrameDuration { get; private set; }

        /// <summary>
        /// Game based seconds from start, pausing or altering the TimeFlowRate will make this value diverge from the real system one.
        /// </summary>
        public PreciseFloat SecondsFromStart	{ get; private set; }	

        /// <summary>
        /// System-time based seconds from start.
        /// </summary>
        public PreciseFloat RealSecondsFromStart { get; private set; }

        public DateTime Now
        {
            get
            {
                return startDate.AddSeconds(SecondsFromStart);
            }
        }

        public float TimeFlowRate { get; set; }

        public int FrameIndex { get; private set; }

        public int FramesPerSecond
        {
            get
            {
                if (FrameIndex == 0)
                    return 0;
                return System.Math.Min(MAX_FPS, (int)(1.0f / RealFrameDuration));
            }
        }
		
		public void Play()
		{
			TimeFlowRate = 1.0f;
            lastUpdateTicks = Stopwatch.GetTimestamp();

        }
		
		public void Stop()
		{
			TimeFlowRate = 0.0f;
		}
		
		internal void NewFrame()
		{
            long curTicks = Stopwatch.GetTimestamp();
            long elapsedTicks = System.Math.Max(1, curTicks - lastUpdateTicks);

            // update real-time
            RealFrameDuration = (float)elapsedTicks / (float)TimeSpan.TicksPerSecond;
            RealSecondsFromStart = RealSecondsFromStart + RealFrameDuration;

            // update game-time
            LastFrameDuration = PredictPrevFrameOnScreenTime(RealFrameDuration) * TimeFlowRate;
            SecondsFromStart = SecondsFromStart + LastFrameDuration;

            // update frame index and timestamp
            FrameIndex = FrameIndex + 1;
            lastUpdateTicks = curTicks;
        }

        private float PredictPrevFrameOnScreenTime(float realFrameDuration)
        {
            if (frameDurationHistory.Count < SMOOTHING_FRAME_COUNT)
            {   
                // fill previous frames hystory
                frameDurationHistory.Add(realFrameDuration);
                return realFrameDuration; // skip filtering if not enough samples have accumulated
            }
            
            // upate frame time history
            frameDurationHistory[FrameIndex % SMOOTHING_FRAME_COUNT] = realFrameDuration;

            // calc a frame time prediction based on the last frames average
            float totalFrameTime = 0;
            for (int i = 0; i < frameDurationHistory.Count; i++)
            {
                totalFrameTime += frameDurationHistory[i];
            }

            return totalFrameTime / SMOOTHING_FRAME_COUNT;
        }
    }
}
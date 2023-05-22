using System;
using System.Text;

namespace Dragonfly.Engine.Core
{
    internal class SceneLog
    {
        private int curframeID, lastLoggedFrameID;
        private string lastFrameLog;
        private StringBuilder curFrameLog;

        public SceneLog()
        {
            curframeID = -1;
            lastLoggedFrameID = -1;
            lastFrameLog = string.Empty;
            curFrameLog = new StringBuilder();
        }

        public void FrameStart(int frameIndex)
        {
            FlushMessages();

            curframeID = frameIndex;
            curFrameLog = new StringBuilder();
        }

        public void WriteLine(string message, params object[] args)
        {
            curFrameLog.AppendFormat(message, args);
            curFrameLog.AppendLine();
        }

        private void FlushMessages()
        {
            if (curframeID == lastLoggedFrameID)
                return;

            
            string frameLog = curFrameLog.ToString();

            // warn the user if the frame is the same of the previous and stop repeating messages
            if (frameLog == lastFrameLog)
            {
                if (curframeID == lastLoggedFrameID + 1)
                {
                    System.Diagnostics.Debug.WriteLine("FRAME #{0}+", curframeID);
                    System.Diagnostics.Debug.WriteLine("Repeating...");
                }
                return;
            }

            // log last frame
            System.Diagnostics.Debug.WriteLine("FRAME #{0}", curframeID);
            System.Diagnostics.Debug.Write(frameLog);
            lastFrameLog = frameLog;
            lastLoggedFrameID = curframeID;
        }

    }
}

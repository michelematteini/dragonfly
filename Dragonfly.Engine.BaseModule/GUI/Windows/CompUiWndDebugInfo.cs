using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;

namespace Dragonfly.BaseModule
{
    public class CompUiWndDebugInfo : Component, ICompUpdatable
    {
        private PreciseFloat lastUpdateSeconds;
        private CircularArray<int> fpsHistory;
        private PreciseFloat startTime;
        private long startFrame;

        public CompUiWindow Window { get; private set; }

        /// <summary>
        /// Minimum FPS among the most recent 
        /// </summary>
        public int FpsMin { get; private set; }

        /// <summary>
        /// Maximum FPS among the most recent 
        /// </summary>
        public int FpsMax { get; private set; }

        /// <summary>
        /// Average FPS of the current session.
        /// </summary>
        public float FpsAvg { get; private set; }

        public int PolygonCount { get; private set; }

        private CompUiCtrlLabel lblCamPos, lblCamTile, lblCamDir, lblFPS, lblCompCount, lblOthers, lblPolyCount, lblDrawCallCount, lblDrawableProcCount;
        private CompUiCtrlGraph fpsGraph;

        public CompUiWndDebugInfo(Component owner) : base(owner)
        {
            FpsAvg = 0;
            startFrame = -1;
            fpsHistory = new CircularArray<int>(30);
            
            lastUpdateSeconds = Context.Time.SecondsFromStart;
            BaseModUiSettings uiSettings = Context.GetModule<BaseMod>().Settings.UI;

            BaseMod baseMod = Context.GetModule<BaseMod>();
            Window = new CompUiWindow(baseMod.UiContainer, "22em 18em", "10px 10px");
            Window.Title = "Rendering Stats";
            Window.PositionLocked = true;
            Window.CloseButtonEnabled = false;
            lblCamPos = new CompUiCtrlLabel(Window, "Camera position: [XXXXXX.XX, YYYYYY.YY, ZZZZZZ.ZZ]", UiPositioning.Inside(Window, "0 0"));
            lblCamTile = new CompUiCtrlLabel(Window, "World Tile: [XXXXXX, YYYYYY, ZZZZZZ]", UiPositioning.Below(lblCamPos));
            lblCamDir = new CompUiCtrlLabel(Window, "Camera direction: [XX.XXX, YY.YYY, ZZ.ZZZ]", UiPositioning.Below(lblCamTile));
            lblFPS = new CompUiCtrlLabel(Window, "FPS: XXXX (Min: XXXX, Max: XXXX, Avg: XXXX)", UiPositioning.Below(lblCamDir));
            lblCompCount = new CompUiCtrlLabel(Window, "Active components: XXXXXX (XXXXXX drawable)", UiPositioning.Below(lblFPS));
            lblPolyCount = new CompUiCtrlLabel(Window, "Polygon count: XXXXXXXX", UiPositioning.Below(lblCompCount));
            lblDrawCallCount = new CompUiCtrlLabel(Window, "Draw calls: XXXXXXXX", UiPositioning.Below(lblPolyCount));
            lblDrawableProcCount = new CompUiCtrlLabel(Window, "Total processed drawables: XXXXXXXXXX", UiPositioning.Below(lblDrawCallCount));
            lblOthers = new CompUiCtrlLabel(Window, "Tasks: XXXX, Frame ID: XXXXXXXXX", UiPositioning.Below(lblDrawableProcCount));
            fpsGraph = new CompUiCtrlGraph(Window, UiPositioning.Below(lblOthers), "16em 4em");
            fpsGraph.TracesAlpha = new Float3(1.0f, 1.0f, 0);
            fpsGraph.FillAlpha = new Float3(0.4f, 0.4f, 0);
            fpsGraph.Color2 = Color.Red.ToFloat4(); // second trace used to mark 30 fps
        }

        public UpdateType NeededUpdates 
        {
            get
            {

                return UpdateType.FrameStart1 | (Context.Time.SecondsFromStart - lastUpdateSeconds > 0.25f ? UpdateType.FrameStart2 : UpdateType.None); 
            } 
        }

        public void Update(UpdateType updateType)
        {
            CompRenderPass mainView = Context.GetModule<BaseMod>().MainPass;
            int fps = Context.Time.FramesPerSecond;

            if (updateType == UpdateType.FrameStart1)
            {
                // update fps counters
                fpsHistory.Shift(-1);
                fpsHistory[0] = Context.Time.FramesPerSecond;
                FpsMin = fpsHistory[0];
                FpsMax = fpsHistory[0];
                for (int i = 1; i < fpsHistory.Length; i++)
                {
                    FpsMin = System.Math.Min(fpsHistory[i], FpsMin);
                    FpsMax = System.Math.Max(fpsHistory[i], FpsMax);
                }

                if (startFrame < 0)
                {
                    FpsAvg = fps;
                    startFrame = Context.Time.FrameIndex;
                    startTime = Context.Time.RealSecondsFromStart;
                }
                else
                {
                    FpsAvg = (Context.Time.FrameIndex - startFrame) / (Context.Time.RealSecondsFromStart - startTime).FloatValue;
                }
            }
            else
            {
                // update labels
                Float3 camPos = mainView.Camera.Position.ToFloat3();
                lblCamPos.Text.InsertLeft(18, 9, camPos.X, 2).InsertLeft(29, 9, camPos.Y, 2).InsertLeft(40, 9, camPos.Z, 2);
                Int3 worldTile = mainView.Camera.Position.Tile;
                lblCamTile.Text.InsertLeft(13, 6, worldTile.X).InsertLeft(21, 6, worldTile.Y).InsertLeft(29, 6, worldTile.Z);
                Float3 camDir = mainView.Camera.Direction;
                lblCamDir.Text.InsertLeft(19, 6, camDir.X, 3).InsertLeft(27, 6, camDir.Y, 3).InsertLeft(35, 6, camDir.Z, 3);
                lblFPS.Text.InsertLeft(5, 4, fps).InsertLeft(16, 4, FpsMin).InsertLeft(27, 4, FpsMax).InsertLeft(38, 4, (int)FpsAvg);
                lblCompCount.Text.InsertLeft(19, 6, Context.Statistics.ComponentCount).InsertLeft(27, 6, Context.Statistics.DrawableCount);
                lblPolyCount.Text.InsertLeft(15, 8, Context.Statistics.LastFrame.PolygonCount);
                lblDrawCallCount.Text.InsertLeft(12, 8, Context.Statistics.LastFrame.DrawCallCount);
                lblDrawableProcCount.Text.InsertLeft(27, 10, Context.Statistics.LastFrame.ProcessedDrawableCount);
                lblOthers.Text.InsertLeft(7, 4, GetComponent<CompTaskScheduler>().LastFrameTaskCount).InsertLeft(23, 9, Context.Time.FrameIndex);

                // update fps graph
                fpsGraph.AddDataPoint(Context.Time.RealSecondsFromStart.FloatValue, fps, 30.0f);
                fpsGraph.DisplayedRange = fpsGraph.DisplayedRange.Add(new Float2(Context.Time.RealSecondsFromStart.FloatValue, 0)); // keep the 0 fps floor in range

                lastUpdateSeconds = Context.Time.SecondsFromStart;
            }
        }
    }
}

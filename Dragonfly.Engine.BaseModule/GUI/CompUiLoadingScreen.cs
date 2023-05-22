using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompUiLoadingScreen : Component, ICompUpdatable
    {
        private CompRenderPass previousPass;
        private bool showRequest, hideRequest;
        private float lastUpdated;
        private CompEvent isLoading;

        public CompUiLoadingScreen(Component parent) : base(parent)
        {
            Pass = new CompRenderPass(this, "LoadingScreen");
            Pass.MainClass = "LoadingScreen";
            Pass.ClearValue = Color.Black.ToFloat4();
            Pass.Camera = new CompCamIdentity(this);
            ShowAutomatically = false;
            HideAutomatically = false;
            UpdateIntervalSeconds = 1.0f;
            lastUpdated = float.MinValue;
            isLoading = new CompEventEngineLoading(this).Event;
            UiPanel = new CompUiContainer(this, new UiRenderPassCanvas(Pass), "100% 100%", "0px 0px", PositionOrigin.TopLeft);
        }

        public CompRenderPass Pass { get; private set; }

        public CompUiContainer UiPanel { get; private set; }

        public UpdateType NeededUpdates
        {
            get
            {
                bool autoUpdateTimerElapsed = Context.Time.RealSecondsFromStart - lastUpdated >= UpdateIntervalSeconds;
                if (showRequest || hideRequest || (autoUpdateTimerElapsed && (HideAutomatically || ShowAutomatically)))
                    return UpdateType.FrameStart1;

                return UpdateType.None;
            }
        }

        public bool ShowAutomatically { get; set; }

        public bool HideAutomatically { get; set; }

        public float UpdateIntervalSeconds { get; set; }

        public bool Visible { get; private set; }

        public void Update(UpdateType updateType)
        {
            lastUpdated = Context.Time.RealSecondsFromStart.FloatValue;
 
            if (isLoading.GetValue() && !Visible && (ShowAutomatically || showRequest))
                ShowLoadingScreen_Internal();
            else if(!isLoading.GetValue() && Visible && (HideAutomatically || hideRequest))
                HideLoadingScreen_Internal();
        }

        public void ShowLoadingScreen()
        {
            if (Visible) return;
            showRequest = true;
            hideRequest = false;
        }

        public void HideLoadingScreen()
        {
            if (!Visible) return;
            showRequest = false;
            hideRequest = true;
        }

        private void ShowLoadingScreen_Internal()
        {
            showRequest = false;
            previousPass = Context.Scene.MainRenderPass;
            Context.Scene.MainRenderPass = Pass;
            Context.Time.Stop();
            Visible = true;
        }

        private void HideLoadingScreen_Internal()
        {
            hideRequest = false;
            Context.Scene.MainRenderPass = previousPass;
            Context.Time.Play();
            Visible = false;
        }



    }
}

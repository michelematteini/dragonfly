using Dragonfly.Engine.Core;
using System;
using System.Windows.Forms;

namespace Dragonfly.Utils.Forms
{
    public interface IControl3D : IWin32Window
    {
        event Action<Exception> EngineErrorOccurred;

        event Action SceneSetup;
     
        bool RenderOnMainThread { get; set; }

        EngineContext Engine { get; }

        string StartupPath { get; set; }

        bool Antialising { get; set; }

        bool CaptureErrors { get; set; }

        void InitializeGraphics();

        void DestroyGraphics();

        TargetControl GetTargetControl();
    }
}

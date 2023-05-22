using Dragonfly.Engine.Core.IO;
using System;
using System.IO;
using System.Collections.Generic;
using Dragonfly.Utils;
using System.Threading;

namespace Dragonfly.Engine.Core
{
	public class EngineContext
	{
        private string resourceFolder;
        private List<EngineModule> modules;
        private bool initialized;
        private object frameLock;

        public bool Released { get; private set; }

        internal EngineContext(EngineParams ep)
        {
            ResourceFolder = ep.ResourceFolder;
            Scene = new Scene(this, ep.Target);
            Scene.Settings.HardwareAntiAliasing = ep.AntiAliasing;
            Time = new Timeline(ep.StartTime);
            Input = new InputDeviceList();
            TargetWindow = ep.Target;
            TargetWindow.CurrentEngine = this;
            modules = new List<EngineModule>();
            frameLock = new object();
            Statistics = new EngineStats(this);
        }
		
		public Scene Scene { get; private set; }
		
		public Timeline Time { get; private set; }

        public InputDeviceList Input { get; private set; }

        public string ResourceFolder
        {
            get { return resourceFolder; }
            private set
            {
                resourceFolder = PathEx.NormalizePath(value);
            }
        }

        public EngineTarget TargetWindow { get; private set; }

        public string GetResourcePath(string resourceName)
        {
            return Path.Combine(resourceFolder, resourceName.ToUpperInvariant());
        }

        /// <summary>
        /// Render a new engine frame.
        /// </summary>
        public bool RenderFrame()
        {
            if (!Monitor.TryEnter(frameLock))
                throw new Exception("RenderFrame() cannot be called recursively while another RenderFrame() call is executing!");

            bool frameRenderSucceeded = false;

            try
            {
                // update time
                if (initialized) // before starting time, the engine should be ready to render.
                    Time.NewFrame();

                if (!initialized)
                {
                    Scene.Initialize();
                    Time.Play();
                    initialized = true;
                }

                // update input devices
                foreach (InputDevice dev in Input.GetAllDevices())
                    dev.NewFrame();

                // render frame
                frameRenderSucceeded = Scene.RenderFrame(); // render to screen or default target
            }
            finally
            {
                Monitor.Exit(frameLock);
            }

            return frameRenderSucceeded;
        }

        /// <summary>
        /// Check if this context is in a valid state and can render.
        /// </summary>
        public bool CanRender { get { return Scene.Graphics.IsAvailable; } }

        public void AddModule(EngineModule module)
        {           
            module.Context = this;
            modules.Add(module);
            module.OnModuleAdded();
        }

        public T GetModule<T>() where T : EngineModule
        {
            foreach(EngineModule m in modules)
            {
                T requiredModule = m as T;
                if (requiredModule != null)
                    return requiredModule; 
            }

            throw new Exception("A component required an unavailable module: " + typeof(T).ToString());
        }

        public EngineStats Statistics { get; private set; }

        public void Release()
        {
            modules.Clear();
            Scene.Release();
            Scene = null;
            TargetWindow = null;
            Released = true;
        }
    }
	
	public struct EngineParams
	{
		public EngineTarget Target;
		public DateTime StartTime;
        public string ResourceFolder;
        public bool AntiAliasing;
	}

    public class UnsupportedComponentException<T> : Exception    {  }
}
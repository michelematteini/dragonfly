using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// A component that returns an up vector that interpolates to the nearest planet gravity.
    /// </summary>
    public class CompPlanetUpVector : Component<Float3>, ICompUpdatable
    {
        private Float3 defaultUp;
        private TiledFloat3 lastViewPos;

        public CompPlanetUpVector(Component owner, Float3 defaultUp) : base(owner)
        {
            this.defaultUp = defaultUp;
            AltitudeOverRadiusBlendStart = 0.3f;
            AltitudeOverRadiusBlendEnd = 0.1f;
        }

        public float AltitudeOverRadiusBlendStart { get; set; }

        public float AltitudeOverRadiusBlendEnd { get; set; }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        public void Update(UpdateType updateType)
        {
            BaseMod baseMod = Context.GetModule<BaseMod>();
            lastViewPos = baseMod.MainPass.Camera.Position;
        }

        protected override Float3 getValue()
        {
            IReadOnlyList<CompPlanet> planets = GetComponents<CompPlanet>();

            if (planets.Count == 0)
                return default;
            
            // find the closest planet (relatively to its radius)      
            CompPlanet closestPlanet = null;
            float closestRelativeDist = float.MaxValue;
            for (int i = 0; i < planets.Count; i++)
            {
                float relativeDist = (lastViewPos - planets[i].Center).Length.ToFloat() / planets[i].Radius.ToFloat();
                if (relativeDist < closestRelativeDist)
                {
                    closestPlanet = planets[i];
                    closestRelativeDist = relativeDist;
                }
            }

            // return the interpolated up vector between the default up and the planet up, based on altitude
            float altitudePercent = closestRelativeDist - 1.0f;
            if (altitudePercent > AltitudeOverRadiusBlendStart)
                return defaultUp;
            Float3 planetUp = (lastViewPos - closestPlanet.Center).ToFloat3().Normal();
            if (altitudePercent < AltitudeOverRadiusBlendEnd)
                return planetUp;            
            return planetUp.SmoothStep(defaultUp, (altitudePercent - AltitudeOverRadiusBlendEnd) / (AltitudeOverRadiusBlendStart - AltitudeOverRadiusBlendEnd)).Normal();
        }
    }
}

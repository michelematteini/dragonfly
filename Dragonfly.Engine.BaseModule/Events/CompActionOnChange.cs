using Dragonfly.Engine.Core;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Perform an acton when the value of the specified component changes
    /// </summary>
    public class CompActionOnChange : Component, ICompUpdatable
    {
        public static CompActionOnChange MonitorValue<T>(Component<T> monitoredComponent, Action<T> onValueChanged)
        {
            CompActionOnChange changeMon = new CompActionOnChange(monitoredComponent, c => onValueChanged((c as Component<T>).GetValue()));
            changeMon.Monitored.Add(monitoredComponent);
            return changeMon;
        }

        public CompActionOnChange(Component parent, Action<Component> onValueChanged = null) : base(parent)
        {
            Monitored = new List<Component>();
            OnValueChanged = onValueChanged;
        }

        public List<Component> Monitored { get; private set; }

        public Action<Component> OnValueChanged;

        /// <summary>
        /// If set to true, the callback is only called once per frame even if multiple components changed their values.
        /// </summary>
        public bool AggregateEvents { get; set; }

        public UpdateType NeededUpdates
        {
            get 
            {
                foreach (Component c in Monitored)
                    if (c.ValueChanged) 
                        return UpdateType.FrameStart2;
                return UpdateType.None;
            }
        }

        public void Execute()
        {
            Update(UpdateType.FrameStart2);
        }

        public void Update(UpdateType updateType)
        {
            if (OnValueChanged == null)
                return;
            foreach (Component c in Monitored)
                if (c.ValueChanged)
                {
                    OnValueChanged(c);
                    if (AggregateEvents)
                        break;
                }
        }
    }

}

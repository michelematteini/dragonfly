using Dragonfly.Engine.Core;
using System;
using System.Threading.Tasks;

namespace Dragonfly.BaseModule
{
    public class CompActionOnEvent : Component, ICompUpdatable
    {
        private Component<bool> eventComp;

        public CompActionOnEvent(Component<bool> eventComponent, Action action, bool async) : base(eventComponent)
        {
            eventComp = eventComponent;
            Async = async;
            Action = action;
        }

        public CompActionOnEvent(Component<bool> eventComponent, Action action) : this(eventComponent, action, false) { }

        /// <summary>
        /// Specify whether the action should be execute synchronously.
        /// </summary>
        public bool Async { get; set; }

        public Action Action { get; private set; }

        public UpdateType NeededUpdates => eventComp.GetValue() ? UpdateType.FrameStart2 : UpdateType.None;

        public bool DisposeOnceExecuted { get; set; }

        public void Update(UpdateType updateType)
        {
            if (Async)
                Task.Run(Action);
            else
                Action();

            if (DisposeOnceExecuted)
                Dispose();
        }
    }
}

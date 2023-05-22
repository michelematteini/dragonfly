using Dragonfly.Engine.Core;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Managers input sharing between components, to avoid multiple components reacting to the same input when this should be avoided.
    /// </summary>
    public class CompInputFocus : Component, ICompUpdatable
    {
        private class InputState
        {
            public bool Available;
            public List<Action> QueuedCallbacks;

            public InputState()
            {
                Available = true;
                QueuedCallbacks = new List<Action>();
            }
        }

        private int lastResetFrame;
        private Dictionary<InputType, InputState> inputStates;

        public CompInputFocus(Component parent) : base(parent)
        {
            inputStates = new Dictionary<InputType, InputState>();
            foreach (object inputType in Enum.GetValues(typeof(InputType)))
                inputStates[(InputType)inputType] = new InputState();
            lastResetFrame = -1;
        }

        /// <summary>
        /// True if mouse input has not been consumed this frame
        /// </summary>
        public bool IsInputAvailable(InputType input)
        {
            ResetOnNewFrame();
            return inputStates[input].Available;
        }

        /// <summary>
        /// Consume mouse input so that no other components use it.
        /// </summary>
        /// <returns>Returns true if the specified input type was available.</returns>
        public bool TryConsumeInput(InputType input)
        {
            ResetOnNewFrame();
            bool wasAvailable = IsInputAvailable(input);
            inputStates[input].Available = false;
            return wasAvailable;
        }

        /// <summary>
        /// Queue a request to execute an action if the specified input was not used in the current frame.
        /// </summary>
        public void RequestInput(InputType input, Action callback)
        {
            ResetOnNewFrame();
            inputStates[input].QueuedCallbacks.Add(callback);
        }

        private void ResetOnNewFrame()
        {
            if(lastResetFrame < Context.Time.FrameIndex)
            {
                foreach (InputState inputState in inputStates.Values)
                {
                    inputState.Available = true;
                    inputState.QueuedCallbacks.Clear();
                }
                lastResetFrame = Context.Time.FrameIndex;
            }
        }

        public UpdateType NeededUpdates => UpdateType.FrameStart2;

        public void Update(UpdateType updateType)
        {
            foreach (KeyValuePair<InputType, InputState> input in inputStates)
            {
                foreach(Action callback in input.Value.QueuedCallbacks)
                {
                    if (!input.Value.Available)
                        break;

                    callback.Invoke();
                }
            }
        }


    }

    public enum InputType
    {
        Mouse,
        Keyboard
    }

}

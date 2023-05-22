using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.Engine.Core
{
    /// <summary>
    /// Manage a command list used to set globals with frame visibility.
    /// </summary>
    public class EngineGlobals
    {
        private CommandList cmdList;

        internal EngineGlobals(CommandList cmdList)
        {
            this.cmdList = cmdList;
            cmdList.FlushRequired = true;
        }

        public void SetParam(string name, bool value)
        {
            cmdList.SetParam(name, value);
        }

        public void SetParam(string name, int value)
        {
            cmdList.SetParam(name, value);
        }

        public void SetParam(string name, float value)
        {
            cmdList.SetParam(name, value);
        }

        public void SetParam(string name, Float2 value)
        {
            cmdList.SetParam(name, value);
        }

        public void SetParam(string name, Float3 values)
        {
            cmdList.SetParam(name, values);
        }

        public void SetParam(string name, Float4 values)
        {
            cmdList.SetParam(name, values);
        }

        public void SetParam(string name, Float4x4 value)
        {
            cmdList.SetParam(name, value);
        }

        public void SetParam(string name, Float3x3 value)
        {
            cmdList.SetParam(name, value);
        }

        public void SetParam(string name, int[] values)
        {
            cmdList.SetParam(name, values);
        }

        public void SetParam(string name, float[] values)
        {
            cmdList.SetParam(name, values);
        }

        public void SetParam(string name, Texture value)
        {
            cmdList.SetParam(name, value);
        }

        public void SetParam(string name, RenderTarget value)
        {
            cmdList.SetParam(name, value);
        }

        public void SetParam(string name, Float2[] values)
        {
            cmdList.SetParam(name, values);
        }

        public void SetParam(string name, Float3[] values)
        {
            cmdList.SetParam(name, values);
        }

        public void SetParam(string name, Float4[] value)
        {
            cmdList.SetParam(name, value);
        }

        public void SetParam(string name, Float4x4[] value)
        {
            cmdList.SetParam(name, value);
        }

        public void Release()
        {
            if (cmdList != null)
                cmdList.Release();
        }
    }

}

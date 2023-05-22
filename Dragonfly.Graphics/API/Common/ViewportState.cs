using Dragonfly.Graphics.Math;

namespace Dragonfly.Graphics.API.Common
{
    internal struct ViewportState
    {
        public static readonly AARect Default = new AARect(0, 0, 1, 1);

        private AARect current;

        public AARect Current
        {
            get { return current; }
            set
            {
                current = value;
                Changed = true;
            }
        }

        public bool Changed { get; set; }

        public void Reset()
        {
            Current = Default;
            Changed = false;
        }
    }
}

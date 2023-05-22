using Dragonfly.Utils;
using Dragonfly.Engine.Core.IO;
using Dragonfly.Engine.Core;
using System.Text;

namespace Dragonfly.BaseModule
{
    public class Keyboard : InputDevice
	{
        private class KeyState
        {
            public int DownCount;
            public bool IsDown;
            public bool WasDown; // whether this key was down in the previous frame

            public KeyState()
            {
                IsDown = false;
                DownCount = 0;
                WasDown = false;
            }

            public void TakeSnapshotAndReset(KeyState snapShot)
            {
                // save snapshot
                snapShot.DownCount = DownCount;
                snapShot.IsDown = IsDown;
                snapShot.WasDown = WasDown;

                // prepare next state
                WasDown = IsDown;
                DownCount = 0;
            }
        }

        private object FRAME_LOCK;
		private KeyState[] keyboard;
        private KeyState[] keyboardSnapShot;
		private EngineTarget inputSource;
				
		public Keyboard(EngineTarget inputSource)
		{
			keyboard = new KeyState[256];
            keyboardSnapShot = new KeyState[256];
            FRAME_LOCK = new object();

            Reset();
            this.inputSource = inputSource;
            inputSource.Activate += Reset;
            inputSource.KeyDown += OnKeyDown;
            inputSource.KeyUp += OnKeyUp;
		}

        public void Reset()
        {
            for (int i = 0; i < 256; i++)
            {
                keyboard[i] = new KeyState();
                keyboardSnapShot[i] = new KeyState();
            }
        }

        public override void Unplug()
		{
            inputSource.Activate -= Reset;
            inputSource.KeyDown -= OnKeyDown;
            inputSource.KeyUp -= OnKeyUp;
        }
		
		public override bool IsKeyDown(VKey key)
		{
            return keyboardSnapShot[(int)key].IsDown;
		}
		
		public bool KeyClicked(VKey key)
		{
            return GetKeyClickCount(key) > 0;
		}

        public bool KeyPressed(VKey key)
        {
            return keyboardSnapShot[(int)key].DownCount > 0;
        }
		
		public int GetKeyClickCount(VKey key)
		{
            KeyState keyState = keyboardSnapShot[(int)key];
            return keyState.DownCount + (keyState.WasDown ? 1 : 0) - (keyState.IsDown ? 1 : 0);
		}
		
		public override void NewFrame()
		{
            lock (FRAME_LOCK)
            {
                for (int i = 0; i < 256; i++)
                    keyboard[i].TakeSnapshotAndReset(keyboardSnapShot[i]);
            }
		}

        private void OnKeyDown(VKey key)
        {
            lock (FRAME_LOCK)
            {
                keyboard[(int)key].IsDown = true;
                keyboard[(int)key].DownCount += 1;
            }
        }

        private void OnKeyUp(VKey key)
        {
            lock (FRAME_LOCK)
            {
                keyboard[(int)key].IsDown = false;
            }
        }

        /// <summary>
        /// Return all the text characted that have been pressed this frame
        /// </summary>
        /// <returns></returns>
        public string GetTextInput()
        {
            StringBuilder sb = new StringBuilder();
            bool shiftPressed = IsKeyDown(VKey.VK_SHIFT);
            for (int i = 0; i < keyboardSnapShot.Length; i++)
            {
                VKey curKey = (VKey)i;
                if (KeyPressed(curKey))
                    sb.Append(curKey.ToKeyString(shiftPressed));
            }
            return sb.ToString();
        }
    }

}



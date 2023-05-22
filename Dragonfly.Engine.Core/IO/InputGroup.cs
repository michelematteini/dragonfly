using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Engine.Core.IO
{
    public class InputDeviceList
    {
        private List<InputDevice> devices;

        internal InputDeviceList()
        {
            devices = new List<InputDevice>();
        }

        public void AddDevice(InputDevice inputDevice)
        {
            devices.Add(inputDevice);
        }

        // This is slow, should be cached by the user.
        public T GetDevice<T>() where T : InputDevice
        {
            T dev = null;
            for (int i = 0; i < devices.Count; i++)
            {
                dev = devices[i] as T;
                if (dev != null) break;
            }
            return dev;
        }

        internal List<InputDevice> GetAllDevices()
        {
            return devices;
        }
    }
}

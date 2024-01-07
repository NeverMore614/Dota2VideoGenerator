using MetaDota.Common.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MetaDota.InputSimulation
{
    internal class KeyboardSimulation : SingleTon<KeyboardSimulation>
    {

        const int KEYEVENTF_EXTENDEDKEY = 0x1;
        const int KEYEVENTF_KEYUP = 0x2;

        public void _KeyboardClick(char key, bool up)
        {

            if (up)
            {

                NativeMethods.keybd_event((byte)key, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);

            }
            else
            {

                NativeMethods.keybd_event((byte)key, 0x45, KEYEVENTF_EXTENDEDKEY, 0);

            }
        }

        public static void KeyboardClick(char c, bool up)
        {
            Instance._KeyboardClick(c, up);
        }
    }
}

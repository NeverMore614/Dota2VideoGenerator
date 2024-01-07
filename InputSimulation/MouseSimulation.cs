using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MetaDota.Common.Native;

namespace MetaDota.InputSimulation
{
    public class MouseSimulation : SingleTon<MouseSimulation>
    {
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        private int _deltaTime = 100;


        public MouseSimulation() { 
        }

        async Task _Click(int x, int y)
        {
            NativeMethods.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, x, y, 0, 0);
            await Task.Delay( 1000 );
        }
        async Task _MoveTo(int x, int y, int time = 0, bool relative = false)
        {
  
            POINT cPOINT = MDTools.GetCursorPosition();
            POINT tPOUNT = new POINT();
            tPOUNT.X = relative ? cPOINT.X + x : x;
            tPOUNT.Y = relative ? cPOINT.Y + y : y;
            if (time <= 0)
            {
                NativeMethods.SetCursorPos(tPOUNT.X, tPOUNT.Y);
            }
            else
            {
                int curTime = 0;
                float t = 0f;
                int xd = 0;
                int yd = 0;
                while (curTime <= time) 
                {
                    t = (float)curTime / (float)time;
                    xd = MDTools.Lerp(cPOINT.X, tPOUNT.X, t);
                    yd = MDTools.Lerp(cPOINT.Y, tPOUNT.Y, t);
     
                    NativeMethods.SetCursorPos(xd, yd);
                    await Task.Delay(_deltaTime);
                    curTime += _deltaTime;
                }
            }

            await Task.Delay(1000);
        }

        public static void Click(int x, int y)
        { 
            Instance._Click(x, y).Wait();
        }
        public static void MoveTo(int x, int y, int time = 0, bool relative = false)
        {
            Instance._MoveTo(x, y, time, relative).Wait();
        }
    }

}

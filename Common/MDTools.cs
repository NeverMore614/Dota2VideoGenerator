using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using MetaDota.Common.Native;

public class MDTools

{

    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

    public static Color GetPixelColor(int x, int y)
    {
        IntPtr hdc = GetDC(IntPtr.Zero);
        uint pixel = GetPixel(hdc, x, y);
        ReleaseDC(IntPtr.Zero, hdc);
        Color color = Color.FromArgb((int)(pixel & 0x000000FF),
                     (int)(pixel & 0x0000FF00) >> 8,
                     (int)(pixel & 0x00FF0000) >> 16);

        return color;
    }

    public static POINT GetCursorPosition()
    {
        POINT pt;
        NativeMethods.GetCursorPos(out pt);
        return pt;
    }

    public static int Lerp(int s, int d, float t)
    { 
        return (int)Math.Floor(s + (d - s) * t);
    }

    const uint ES_SYSTEM_REQUIRED = 0x00000001;
    const uint ES_DISPLAY_REQUIRED = 0x00000002;
    const uint ES_CONTINUOUS = 0x80000000;

    /// <summary>
    /// stop pc sleep
    /// </summary>
    /// <param name="sleepOrNot"></param>
    public static void SleepCtr(bool sleepOrNot)
    {
        if (sleepOrNot)
        {
            //阻止休眠时调用
            NativeMethods.SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
        }
        else
        {
            //恢复休眠时调用
            NativeMethods.SetThreadExecutionState(ES_CONTINUOUS);
        }
    }

}

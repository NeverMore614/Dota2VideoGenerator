using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaDota.Common.Native;
using System.Runtime.InteropServices;
using WindowsInput;
using WindowsInput.Native;
using MetaDota.InputSimulation;
using Interceptor;

namespace MetaDota.DotaReplay
{
    internal class MDMovieMaker : SingleTon<MDMovieMaker>
    {

        private Task _task;
        private Input _input;
        static Queue<Task> _task_queue;

        public bool Init()
        {
            try
            {
                _input = new Input();
                _input.KeyboardFilterMode = KeyboardFilterMode.All;
                _input.Load();
                Console.Write("To Start DirectX Input, please enter any key:");
                Console.ReadLine();
                Console.Write("MDMovieMaker Init Success");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("MDMovieMaker Init Fail:" + e.Message);
                return false;
            }

            _StartWorking();
        }

        public async Task _StartWorking()
        {
            _task_queue = new Queue<Task>();
            while (true)
            {
                await Task.Delay(5000);
                if (_task_queue.Count > 0)
                {
                    Task task = _task_queue.Dequeue();
                    task.Start();
                    await task;
                }
            }
        }

        public void CancelRecording()
        {
            if (!DotaClient.Instance.IsInit)
            {
                Console.WriteLine("DotaClient  Is not Init");
                return;
            }

            string playDemoCmd = $"playdemo replays/12312\nshowConsole";

            Process[] processes = Process.GetProcessesByName("dota2");
            if (processes.Length == 0)
            {
                File.WriteAllText(Path.Combine(DotaClient.dotaPath, "game/dota/cfg/autoexec.cfg"), playDemoCmd);

                Process process = new Process();
                process.StartInfo.FileName = DotaClient.dotaLauncherPath;
                process.StartInfo.Arguments = "-console";
                process.Start();
            }
            else
            {
                Console.WriteLine($"222 {processes.Length}");
                Task.Run(() =>
                {
                    Input input = new Input();
                    input.Load();
                    Thread.Sleep(3000);
                    input.SendKey(Keys.G, KeyState.Down);
                });
                NativeMethods.SwitchToThisWindow(processes[0].MainWindowHandle, true);
                Thread.Sleep(1000);
                Input input = new Input();
                input.Load();
                Thread.Sleep(3000);

                input.SendKey(Keys.BackslashPipe, KeyState.Down);

                //MouseSimulation.MoveTo(250, 150);
                //MouseSimulation.Click(0,0);
                Thread.Sleep(500);
                //input.SendKeys(Keys.Enter);
                //inputSimulation.Keyboard.Sleep(2000);
                //inputSimulation.Keyboard.KeyPress(VirtualKeyCode.OEM_5);
                //inputSimulation.Keyboard.Sleep(1000);
                //inputSimulation.Keyboard.TextEntry(playDemoCmd);
                //inputSimulation.Keyboard.Sleep(500);
                //inputSimulation.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }
        }

        public static Task StartRecordMovie()
        {
            if (Instance._task == null || Instance._task.IsCompleted)
            {
                Instance._task = Instance._StartRecordMovie();
            }
            return Instance._task;
        }


    }
}

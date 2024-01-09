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
    internal class MDMovieMaker : MDFactory<MDMovieMaker>
    {

        private Input _input;
        private string _cfgFilePath = "";
        private string _keyFilePath = "";


        public override async Task Init()
        {
            base.Init();
            try
            {
                _input = new Input();
                _input.KeyboardFilterMode = KeyboardFilterMode.All;
                _input.Load();
                Console.Write("To Start DirectX Input, please enter any key:");
                Console.ReadLine();
                Console.Write("MDMovieMaker Init Success");
            }
            catch (Exception e)
            {
                Console.WriteLine("MDMovieMaker Init Fail:" + e.Message);
            }

        }

        public override async Task Work(MDReplayGenerator generator)
        {
            _cfgFilePath = Path.Combine(ClientParams.REPLAY_CFG_DIR, "/replayCfg.txt");
            _keyFilePath = Path.Combine(ClientParams.REPLAY_CFG_DIR, "/keyCfg.txt");
            if (CancelRecording(generator));
            { 
                
            }

            generator.block = false;
        }

        public bool CancelRecording(MDReplayGenerator generator)
        {
            if (!File.Exists(generator.demoFilePath))
            {
                generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DemoDownloadFail;
                return false;
            }
            else if (!File.Exists(_cfgFilePath) || (!File.Exists(_keyFilePath)))
            {
                generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.AnalystFail;
                return false;
            }

            File.Copy(generator.demoFilePath, $"{DotaClient.dotaReplayPath}/{generator.match_id}.dem");
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




    }
}

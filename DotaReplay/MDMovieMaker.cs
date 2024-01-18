using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaDota.Common.Native;
using System.Runtime.InteropServices;
using Interceptor;
using System.Drawing;
using static Dota2.GC.Dota.Internal.CMsgGCToClientSocialMatchDetailsResponse;
using static SteamKit2.GC.Dota.Internal.CMsgSteamLearn_InferenceBackend_Response;
using static SteamKit2.Internal.CContentBuilder_CommitAppBuild_Request;
using SteamKit2;
using static SteamKit2.GC.Dota.Internal.CDOTAMatchMetadata;
using System.Numerics;

namespace MetaDota.DotaReplay
{
    internal class MDMovieMaker : MDFactory<MDMovieMaker>
    {

        public Input _input;
        private string _cfgFilePath = "";
        private string _keyFilePath = "";

        private Dictionary<string, Interceptor.Keys> s2k;

        public override async Task Init()
        {
            base.Init();
            //check drivers
            bool driverInstalled = false;
            DirectoryInfo driverDi = new DirectoryInfo(@"C:\Windows\System32\drivers");
            FileInfo[] fis = driverDi.GetFiles("*.sys");
            foreach (FileInfo fi in fis)
            {
                if (fi.Name == "keyboard.sys")
                {
                    driverInstalled = true;
                    break;
                }
            }
            if (!driverInstalled)
            {

                Console.Write("please install driver interception with install-interception.exe  ");
                Console.ReadLine();
                Environment.Exit(0);
                return;
            }

            try
            {
                _input = new Input();
                _input.KeyboardFilterMode = KeyboardFilterMode.All;
                _input.Load();
                Console.Write("To Start DirectX Input, please enter any key:");
                Console.ReadLine();
                Console.Write("MDMovieMaker Init Success");
                s2k = new Dictionary<string, Interceptor.Keys>() {
                {"kp_0",Interceptor.Keys.Numpad0 },
                {"kp_1",Interceptor.Keys.Numpad1 },
                {"kp_2",Interceptor.Keys.Numpad2 },
                {"kp_3",Interceptor.Keys.Numpad3 },
                {"kp_4",Interceptor.Keys.Numpad4 },
                {"kp_5",Interceptor.Keys.Numpad5 },
                {"kp_6",Interceptor.Keys.Numpad6 },
                {"kp_7",Interceptor.Keys.Numpad7 },
                {"kp_8",Interceptor.Keys.Numpad8 },
                {"kp_9",Interceptor.Keys.Numpad9 },
                {"kp_multiply",Interceptor.Keys.NumpadAsterisk },
                {"kp_minus",Interceptor.Keys.NumpadMinus },
                {"kp_plus",Interceptor.Keys.NumpadPlus },
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("MDMovieMaker Init Fail:" + e.Message);
            }

        }

        public override async Task Work(MDReplayGenerator generator)
        {
            _cfgFilePath = Path.Combine(ClientParams.REPLAY_CFG_DIR, "replayCfg.txt");
            _keyFilePath = Path.Combine(ClientParams.REPLAY_CFG_DIR, "keyCfg.txt");
            if (CancelRecording(generator))
            {
                await Task.Delay(3000);

                while (Process.GetProcessesByName("dota2").Length == 0)
                {
                    await Task.Delay(500);
                }
                RECT rECT = new RECT();
                if (!NativeMethods.GetWindowRect(Process.GetProcessesByName("dota2")[0].MainWindowHandle, ref rECT))
                {
                    generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.LaunchDotaFail;
                    return;
                }
                int centerX = rECT.Left + (rECT.Right - rECT.Left) / 2;
                int centerY = rECT.Top + (rECT.Bottom - rECT.Top) / 2;
                Color pixelColor = MDTools.GetPixelColor(centerX, centerY);
                int sameCount = 10;
                while (sameCount > 0)
                {
                    Color curColor = MDTools.GetPixelColor(centerX, centerY);
                    Console.WriteLine($"{curColor.ToArgb()} {centerX} {centerY}");
                    if (curColor.ToArgb() != pixelColor.ToArgb())
                    {
                        pixelColor = curColor;
                        sameCount--;
                    }
                    await Task.Delay(500);
                }
                string[] keyLines = File.ReadAllLines(_keyFilePath);
                //check is in demo
                _input.SendKey(Interceptor.Keys.BackslashPipe, KeyState.Down);
                _input.SendKey(Interceptor.Keys.BackslashPipe, KeyState.Up);
                _input.SendText("exec replayCfg.txt");
                _input.SendKey(Interceptor.Keys.Enter, KeyState.Down);
                _input.SendKey(Interceptor.Keys.Enter, KeyState.Up);

                string clipFile = "";
                for (int i = 0; i < keyLines.Length; i++)
                {
                    string[] fileKey = keyLines[i].Split('$');
                    if (fileKey.Length == 2)
                    {
                        clipFile = Path.Combine(DotaClient.dotaMoviePath, fileKey[0]);
                        while (!File.Exists(clipFile))
                        {
                            Task.Delay(500);
                        }
                        if (s2k.ContainsKey(fileKey[1]))
                        {
                            _input.SendKey(s2k[fileKey[1]]);
                        }
                        else
                        {
                            _input.SendText(fileKey[1]);
                        }
                    }
                }

                using (Process zipProcess = new Process())
                {
                    zipProcess.StartInfo.FileName = "ffmpeg.exe";
                    zipProcess.StartInfo.UseShellExecute = false;
                    zipProcess.StartInfo.RedirectStandardInput = true;
                    zipProcess.StartInfo.Arguments = $"-y -r 30 -i {Path.GetFullPath(DotaClient.dotaMoviePath)}\\%04d.jpg -i {Path.GetFullPath(DotaClient.dotaMoviePath)}\\.wav replays\\{generator.match_id}_{generator.account_id}.mp4";
                    zipProcess.Start();
                    zipProcess.WaitForExit();
                }

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

            //move file and delete movie file
            File.Copy(generator.demoFilePath, $"{DotaClient.dotaReplayPath}/{generator.match_id}.dem", true);
            File.Copy(_cfgFilePath, $"{DotaClient.dotaCfgPath}/replayCfg.txt", true);
            if (!Directory.Exists(DotaClient.dotaMoviePath))
            {
                Directory.CreateDirectory(DotaClient.dotaMoviePath);
            }
            //Delete movie clip file
            Console.WriteLine("delete movie file ing ...");
            foreach (String file in Directory.GetFiles(DotaClient.dotaMoviePath))
            {
                File.Delete(file);
            }
            Console.WriteLine("delete movie file ing over");



            string playDemoCmd = $"playdemo replays/{generator.match_id}\nhideConsole";
            File.WriteAllText(Path.Combine(DotaClient.dotaCfgPath, "autoexec.cfg"), playDemoCmd);

            Process[] processes = Process.GetProcessesByName("dota2");
            if (processes.Length == 0)
            {
                Process process = new Process();
                process.StartInfo.FileName = DotaClient.dotaLauncherPath;
                process.StartInfo.Arguments = "-console";
                process.Start();
            }
            else
            {
                NativeMethods.SwitchToThisWindow(processes[0].MainWindowHandle, true);
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    _input.SendKey(Interceptor.Keys.BackslashPipe, KeyState.Down);
                    _input.SendKey(Interceptor.Keys.BackslashPipe, KeyState.Up);
                    _input.SendText($"exec autoexec.cfg");
                    _input.SendKey(Interceptor.Keys.Enter, KeyState.Down);
                    _input.SendKey(Interceptor.Keys.Enter, KeyState.Up);
                    await Task.Delay(3000);
                }).Wait();
            }

            return true;
        }




    }
}

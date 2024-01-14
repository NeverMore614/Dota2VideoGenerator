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

namespace MetaDota.DotaReplay
{
    internal class MDMovieMaker : MDFactory<MDMovieMaker>
    {

        public Input _input;
        private string _cfgFilePath = "";
        private string _keyFilePath = "";

        private Dictionary<string, Keys> s2k;

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
                Process process = new Process();
                process.StartInfo.FileName = "install-interception.exe";
                process.StartInfo.Arguments = "/install";
                process.Start();
                process.WaitForExit();
                Console.Write("driver install success, you need restart the computer");
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
                s2k = new Dictionary<string, Keys>() {
                {"kp_0",Keys.Numpad0 },
                {"kp_1",Keys.Numpad1 },
                {"kp_2",Keys.Numpad2 },
                {"kp_3",Keys.Numpad3 },
                {"kp_4",Keys.Numpad4 },
                {"kp_5",Keys.Numpad5 },
                {"kp_6",Keys.Numpad6 },
                {"kp_7",Keys.Numpad7 },
                {"kp_8",Keys.Numpad8 },
                {"kp_9",Keys.Numpad9 },
                {"kp_multiply",Keys.NumpadAsterisk },
                {"kp_minus",Keys.NumpadMinus },
                {"kp_plus",Keys.NumpadPlus },
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

                //check is in demo
                Color pixelColor = MDTools.GetPixelColor(131, 77);
                while (pixelColor.ToArgb() != -1840390)
                {
                    await Task.Delay(1000);
                    pixelColor = MDTools.GetPixelColor(131, 77);
                }
                _input.SendText("exec replayCfg.txt");
                _input.SendKey(Keys.Enter, KeyState.Down);
                string[] keyLines = File.ReadAllLines(_keyFilePath);
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

            File.Copy(generator.demoFilePath, $"{DotaClient.dotaReplayPath}/{generator.match_id}.dem", true);
            File.Copy(_cfgFilePath, $"{DotaClient.dotaCfgPath}/replayCfg.txt", true);

            string playDemoCmd = $"playdemo replays/{generator.match_id}\nshowConsole";

            Process[] processes = Process.GetProcessesByName("dota2");
            if (processes.Length == 0)
            {
                File.WriteAllText(Path.Combine(DotaClient.dotaCfgPath, "autoexec.cfg"), playDemoCmd);
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
                    _input.SendKey(Keys.BackslashPipe, KeyState.Down);
                    _input.SendText($"playdemo replays/{generator.match_id}");
                    _input.SendKey(Keys.Enter, KeyState.Down);
                }).Wait();
            }

            return true;
        }




    }
}

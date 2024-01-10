
using SteamKit2.GC.Dota.Internal;
using SteamKit2;
using MetaDota.DotaReplay;
using System.Net;
using System.Text;
using System.Reflection;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using WindowsInput;
using MetaDota.InputSimulation;
using MetaDota.Common;
using Interceptor;
using MetaDota.Common.Native;
using System.Drawing;


namespace ConsoleApp2
{
    class Program
    {
        public static Queue<string> requestQueue;
        public async static Task Main(string[] args)
        {
#if DEBUG



            //string dotaPath = "E:\\Steam\\steam\\steamapps\\common\\dota 2 beta";
            //await CheckColor();
            string dotaPath = File.ReadAllText("config/dota2Path.txt");
            Console.WriteLine($"dota2 path :{dotaPath}");

#endif

            MDFile.Init();

            MDSever.Instance.Start();

            //movie maker
            MDMovieMaker.Instance.Init();

            //demo downloader
            MDReplayDownloader.Instance.Init();

            MDDotaClientRequestor.Instance.Init();

            MDDemoAnalystor.Instance.Init();

            //dota client
            DotaClient.Instance.Init(dotaPath);


            DotaClient.Instance.Reconnect();
            if (!DotaClient.Instance.IsLogonDota)
            { 
                Console.WriteLine("dota2 launch fail");
                return;
            }

            Console.WriteLine("dota2 launch success! start check replay");

            CheckDownloadTask().Wait();

            Console.WriteLine("Dota Client DisConnected, Please ReConnect");
            Console.ReadLine();
        }


        static async Task CheckColor()
        {
            MDMovieMaker.Instance.Init();

            //Process[] processes = Process.GetProcessesByName("dota2");
            //NativeMethods.SwitchToThisWindow(processes[0].MainWindowHandle, true);
            //MDMovieMaker.Instance._input.SendText(@"\");
            Color color = Color.White;
            while (true)
            { 
                await Task.Delay(1000);
                POINT cPOINT = MDTools.GetCursorPosition();
                color = MDTools.GetPixelColor(cPOINT.X, cPOINT.Y);
                Console.WriteLine($"x:{cPOINT.X} y:{cPOINT.Y} color : {color.ToArgb()}");
            }
        }

        static async Task CheckDownloadTask()
        {
            requestQueue = new Queue<string>();
            string[] requestArry = File.ReadAllLines(ClientParams.MATCH_REQUEST_FILE);
            for (int i = 0; i < requestArry.Length; i++)
            {
                requestQueue.Enqueue(requestArry[i]);
            }
            string requestStr = "";
            while (true)
            {
                await Task.Delay(ClientParams.DOWNLOAD_CHECK_INTERVAL);
                if (requestQueue.Count > 0)
                {
                    requestStr = requestQueue.Dequeue();
                    if (!string.IsNullOrEmpty(requestStr))
                    {
                        MDReplayGenerator.Generate(requestStr);
                        Console.WriteLine($"result : {MDReplayGenerator.GetResult(requestStr)}");
                    }

                }
            }
        }

    }
}


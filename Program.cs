
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


namespace ConsoleApp2
{
    class Program
    {
        public async static Task Main(string[] args)
        {
#if DEBUG
            string dotaPath = "E:\\Steam\\steamapps\\common\\dota 2 beta";
#else
            Console.WriteLine("please input dota2 beta path :");
            string dotaPath = Console.ReadLine();
#endif

            MDFile.Init();

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

            //CMsgDOTAMatch match = MDReplayGenerator.GetMatch(7514943728);
            //if (match != null)
            //{
            //    string a, b, c;
            //    _prepareAnalystParams(match, out a, out b, out c);
            //    Console.WriteLine($"getDetails :{a}  {b}  {c}");
            //}

            CheckDownloadTask().Wait();

            Console.WriteLine("Dota Client DisConnected, Please ReConnect");
            Console.ReadLine();


            //ReplayDownloader.Download(7511635609);
            //MouseSimulation.MoveTo(-1500, 0, 1000, true);
            //MouseSimulation.Click(0, 0);
            //KeyboardSimulation.KeyboardClick('A', false);
            //
            //System.Drawing.Color color = Tools.GetPixelColor(999, 999);
            //Console.WriteLine($"Color r{color.R} g{color.G} b{color.B}");

            //InputSimulator inputSimulator = new InputSimulator();
            //
            //inputSimulator.Mouse.MoveMouseTo(1000, 1000);


        }


        static async Task CheckDownloadTask()
        {
            string requestStr = "";
#if DEBUG
            await Task.Delay(ClientParams.DOWNLOAD_CHECK_INTERVAL);
            MDFile.ReadLine(ClientParams.MATCH_REQUEST_FILE, ref requestStr);
            MDReplayGenerator.Generate(requestStr);
            Console.WriteLine($"result : {MDReplayGenerator.GetResult(requestStr)}");
#else
            while (true)
            {
                await Task.Delay(ClientParams.DOWNLOAD_CHECK_INTERVAL);
                MDFile.ReadLine(ClientParams.MATCH_REQUEST_FILE, ref requestStr);
                MDReplayGenerator.Generate(requestStr);
                Console.WriteLine($"result : {MDReplayGenerator.GetResult(requestStr)}");
            }
#endif
        }

    }
}


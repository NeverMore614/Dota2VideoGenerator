
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


namespace ConsoleApp2
{
    class Program
    {
        
        public static void Main(string[] args)
        {
            Console.WriteLine("please input dota2 beta path :");
            string dotaPath = Console.ReadLine();

            MDReplayGenerator.Instance.Init(dotaPath);
            DotaClient.Instance.Init();

            if (!DotaClient.Instance.IsLogonDota)
            { 
                Console.WriteLine("dota2 launch fail");
                return;
            }

            Console.WriteLine("dota2 launch success! start check replay");

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

;
            //if (match == null) {
            //    Console.WriteLine("未找到match");
            //    return;
            //}
            //Console.WriteLine("找到match");
            //foreach (CMsgDOTAMatch.Player player in match.players)
            //{
            //    Console.WriteLine(player.player_name + "use hero:" + player.hero_id);
            //}
            //DotaClient client = new DotaClient();
            //var id = uint.Parse(args[0] ?? "0");
            //Console.WriteLine(client.GetHeroNameByID(id)); ;
        }

        static async Task CheckDownloadTask()
        {
            string requestStr = "";
            while (true)
            {
                await Task.Delay(ClientParams.DOWNLOAD_CHECK_INTERVAL);
#if DEBUG
                MDFile.ReadLine(ClientParams.MATCH_REQUEST_FILE, ref requestStr);
#endif
                if (MDReplayGenerator.Generate(requestStr))
                {
                    Console.WriteLine($"result : {MDReplayGenerator.GetResult()}");
                }
            }
        }



    }
}


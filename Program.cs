
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
#if DEBUG
            string dotaPath = "E:\\Steam\\steam\\steamapps\\common\\dota 2 beta";
#else
            Console.WriteLine("please input dota2 beta path :");
            string dotaPath = Console.ReadLine();
#endif

            MDFile.Init();

            MDReplayGenerator.Instance.Init(dotaPath);

            DotaClient.Instance.Init();

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

        static bool _prepareAnalystParams(CMsgDOTAMatch matchInfo, out string hero_name, out string slot, out string war_fog)
        {
            hero_name = "";
            slot = "";
            war_fog = "";
            foreach (CMsgDOTAMatch.Player player in matchInfo.players)
            {
                if (player.account_id == 303440494)
                {

                    hero_name = DotaClient.Instance.GetHeroNameByID(player.hero_id);
                    Console.WriteLine($"hero_name = {hero_name}");
                    slot = player.player_slot.ToString();
                    Console.WriteLine($"player_slot = {slot}");
                    Console.WriteLine($"team_slot = {player.team_slot}");
                    war_fog = "";
                    return true;
                }
            }
            return false;
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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using SteamKit2.GC.Dota.Internal;
using System.Threading.Tasks;
using static MetaDota.DotaReplay.MDReplayGenerator;
using static SteamKit2.GC.Dota.Internal.CMsgProfileResponse;

namespace MetaDota.DotaReplay
{
    internal class MDDemoAnalystor : MDFactory<MDDemoAnalystor>
    {
        public override async Task Work(MDReplayGenerator generator)
        {
            CMsgDOTAMatch match = generator.match;

            if (match == null)
            {
                generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.NoMatch;
            }
            else if (!File.Exists(generator.demoFilePath))
            {
                generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DemoDownloadFail;
            }
            else
            {
                string hero_name, slot, war_fog;
                if (!_prepareAnalystParams(generator, out hero_name, out slot, out war_fog))
                {
                    generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.NotFindPlayer;
                }
                else if (!_analyst_demo(generator.demoFilePath, hero_name, slot, war_fog))
                {
                    generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.AnalystFail;
                }

            }

            generator.block = false;
        }

        bool _analyst_demo(string demoFilePath, string hero_name, string slot, string war_fog)
        {
            foreach (String file in Directory.GetFiles(ClientParams.REPLAY_CFG_DIR))
            {
                File.Delete(file);
            }

            using (Process demoP = new Process())
            {
                demoP.StartInfo.FileName = "demo.exe";
                demoP.StartInfo.UseShellExecute = false;
                demoP.StartInfo.RedirectStandardInput = true;
                demoP.StartInfo.Arguments = $"{demoFilePath} {hero_name} {slot} {war_fog}";
                demoP.Start();
                demoP.WaitForExit();
                Console.WriteLine("demo analyst success");
            }
            Console.WriteLine("demo analyst over");
            return File.Exists(ClientParams.REPLAY_CFG_DIR + "/replayCfg.txt") && File.Exists(ClientParams.REPLAY_CFG_DIR + "/keyCfg.txt");

        }

        /// <summary>
        /// prepare analyst params by CMsgDOTAMatch
        /// </summary>
        /// <param name="matchInfo"></param>
        /// <param name="hero_name"></param>
        /// <param name="slot"></param>
        /// <param name="war_fog"></param>
        bool _prepareAnalystParams(MDReplayGenerator generator, out string hero_name, out string slot, out string war_fog)
        {
            hero_name = "";
            slot = "";
            war_fog = "";
            foreach (CMsgDOTAMatch.Player player in generator.match.players)
            {
                if (player.account_id == generator.account_id)
                {
                    hero_name = DotaClient.Instance.GetHeroNameByID(player.hero_id);
                    slot = (player.team_slot + (player.player_slot > 100 ? 5 : 0)).ToString();
                    war_fog = (player.player_slot > 100 ? 3 : 2).ToString();
                    return true;
                }
            }
            return false;
        }

    }
}

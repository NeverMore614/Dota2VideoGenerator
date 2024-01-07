using MetaDota.Common;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static SteamKit2.GC.Dota.Internal.CDOTAMatchMetadata;

namespace MetaDota.DotaReplay
{
    public class MDReplayGenerator : SingleTon<MDReplayGenerator>
    {
        //GenerateTaskResultEnum
        public enum EReplayGenerateResult
        { 
            NoTask,
            NotComplet,
            LaunchDotaFail,
            NoMatch,
            DemoUnavailable,
            DemoDownloadFail,
            NotFindPlayer,
            AnalystFail,
            Success,
            Failure,
        }
        private DotaClient _client;

        public static ulong match_id = 0;
        public static uint account_id = 0;

        private Task<EReplayGenerateResult> _downloadTask;
        public MDReplayGenerator()
        {
            _client = DotaClient.Instance;
        }

        /// <summary>
        /// Init dota path
        /// </summary>
        public void Init()
        {

        }

        bool _Generate(string request)
        {
            if (string.IsNullOrEmpty(request))
            {
                Console.WriteLine($"Parse requset fail :EmptyOrNull");
                return false;

            }
            if (!_client.IsLogonDota)
            {
                Console.WriteLine("Unable to Connect DotaServer");
                return false;
                //_client.Reconnect();
                //await _Download(match_id);
            }

            if (!IsGenerateIdle())
            {
                Console.WriteLine("Task is processing");
                return false;
            }

            //paris request content
            string[] splitArray = request.Split("_");
            try
            {
                match_id = ulong.Parse(splitArray[0]);
                account_id = uint.Parse(splitArray[1]);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Parse {request} fail :{e.Message}");
                return false;
            }

            //download dota replay demo

            _downloadTask = _GenerateMatchReplayTask();
            _downloadTask.Wait();

            return true;
        }

        EReplayGenerateResult _GetResult()
        {
            if (_downloadTask == null) return EReplayGenerateResult.NoTask;

            if (!_downloadTask.IsCompleted) return EReplayGenerateResult.NotComplet;

            return _downloadTask.Result;

        }

        async Task<EReplayGenerateResult> _GenerateMatchReplayTask()
        {
            string destFilePath = Path.Combine(ClientParams.REPLAY_DIR, string.Format("{0}_{1}.mp4", match_id, account_id));

            if (MDFile.FileExists(destFilePath))  return EReplayGenerateResult.Success; 

            //find or get .dem file
            string demoFilePath = Path.Combine(ClientParams.DEMO_DIR, string.Format("{0}.dem", match_id));

            CMsgDOTAMatch matchInfo = await _GetMatch();

            if (matchInfo == null)
                return EReplayGenerateResult.NoMatch;

            if (matchInfo.replay_state != CMsgDOTAMatch.ReplayState.REPLAY_AVAILABLE)
                return EReplayGenerateResult.DemoUnavailable;

            //download demo
            await MDReplayDownloader._DownLoadReplay(matchInfo, demoFilePath);

            //demo download fail
            if (!MDFile.FileExists(demoFilePath)) 
                return EReplayGenerateResult.DemoDownloadFail;



            //prepare demo analyst params
            string hero_name, slot, war_fog;
            if (!_prepareAnalystParams(matchInfo, out hero_name, out slot, out war_fog))
                return EReplayGenerateResult.NotFindPlayer;

            //demo analyst
            if (!_analyst_demo(demoFilePath, hero_name, slot, war_fog))
                return EReplayGenerateResult.AnalystFail;

            MDMovieMaker.Instance.CancelRecording();
            await MDMovieMaker.StartRecordMovie();


            return EReplayGenerateResult.Success;
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
                demoP.StartInfo.RedirectStandardInput = true;
                demoP.StartInfo.Arguments = $"{demoFilePath} {hero_name} {slot} {war_fog}";
                demoP.Start();
                demoP.WaitForExit();
            }
            
            return File.Exists(ClientParams.REPLAY_CFG_DIR + "/replayCfg.txt") && File.Exists(ClientParams.REPLAY_CFG_DIR + "/keyCfg.txt");

        }

        /// <summary>
        /// prepare analyst params by CMsgDOTAMatch
        /// </summary>
        /// <param name="matchInfo"></param>
        /// <param name="hero_name"></param>
        /// <param name="slot"></param>
        /// <param name="war_fog"></param>
        bool _prepareAnalystParams(CMsgDOTAMatch matchInfo, out string hero_name, out string slot, out string war_fog)
        {
            hero_name = "";
            slot = "";
            war_fog = "";
            foreach (CMsgDOTAMatch.Player player in matchInfo.players) {
                if (player.account_id == account_id)
                {
                    hero_name = _client.GetHeroNameByID(player.hero_id);
                    slot = (player.team_slot + (player.player_slot > 100 ? 5 : 0)).ToString();
                    war_fog = (player.player_slot > 100 ? 3 : 2).ToString();
                    return true;
                }
            }
            return false;
        }


        async Task<CMsgDOTAMatch> _GetMatch()
        {
            if (!_client.IsLogonDota)
            {
                Console.WriteLine("未能链接到steam网络，退出");
                return null;
                //_client.Reconnect();
                //await _Download(match_id);
            }
            else
            {
                _client.RequestMatch(match_id);
                _client.WaitMatch();
                return _client.Match;
            }
        }





        public static CMsgDOTAMatch GetMatch(ulong find_match_id)
        {
           match_id = find_match_id;
           Task<CMsgDOTAMatch> task = Instance._GetMatch();
           task.Wait();
           return task.Result;
        }

        public static bool IsGenerateIdle()
        {
            if (Instance._downloadTask == null) return true;
            return Instance._downloadTask.IsCompleted;
        }

        public static bool Generate(string request)
        {
            return Instance._Generate(request);
        }

        public static EReplayGenerateResult GetResult()
        { 
            return Instance._GetResult();
        }
    }
}

using MetaDota.Common;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
            Success,
            Failure,
        }
        private DotaClient _client;
        private string dotaLauncherPath = "game/bin/win64/dota2.exe";
        private string dotaMoviePath = "../movie";
        private ulong match_id = 0;
        private ulong account_id = 0;

        private Task<EReplayGenerateResult> _downloadTask;
        public MDReplayGenerator()
        {
            _client = DotaClient.Instance;
        }

        /// <summary>
        /// Init dota path
        /// </summary>
        /// <param name="dotaPath">dota 2 beta directory path</param>
        public void Init(string dotaPath)
        {
            dotaLauncherPath = Path.Combine(dotaPath, dotaLauncherPath);
            dotaMoviePath = Path.Combine(dotaPath, dotaMoviePath);
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
                account_id = ulong.Parse(splitArray[1]);
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
            _prepareAnalystParams(out hero_name, out slot, out war_fog);


            return EReplayGenerateResult.Success;
        }

        void _prepareAnalystParams(out string hero_name, out string slot, out string war_fog)
        {
            
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





        public static CMsgDOTAMatch GetMatch(ulong match_id)
        {
           Instance.match_id = match_id;
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

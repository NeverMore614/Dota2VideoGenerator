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
using static SteamKit2.GC.Dota.Internal.CMsgSDOAssert;

namespace MetaDota.DotaReplay
{
    public class MDReplayGenerator
    {
        //generator task Factory
        private static Dictionary<string, MDReplayGenerator> sMDReplayGeneratorMap = new Dictionary<string, MDReplayGenerator>();

        //GenerateTaskResultEnum
        public enum EReplayGenerateResult
        { 
            NoTask,
            NotComplet,
            DisConnectServer,
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

        private string _request = "";
        public ulong match_id = 0;
        public uint account_id = 0;
        public CMsgDOTAMatch match;
        public string demoFilePath = "";
        public string replayFilePath = "";
        public string replayResultFilePath = "";

        public bool block = false;
        public EReplayGenerateResult eReplayGenerateResult = EReplayGenerateResult.Success;


        private IMDFactory[] mDFactories;

        public MDReplayGenerator(string request)
        {
            _client = DotaClient.Instance;
            _request = request;
            sMDReplayGeneratorMap.Add(request, this);
        }


        private Task<EReplayGenerateResult> _downloadTask;

         bool _Generate()
        {
            if (string.IsNullOrEmpty(_request))
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
            string[] splitArray = _request.Split('_');
            try
            {
                match_id = ulong.Parse(splitArray[0]);
                account_id = uint.Parse(splitArray[1]);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Parse {_request} fail :{e.Message}");
                return false;
            }

            _prepareTask();

            _downloadTask = _GenerateMatchReplayTask();

            return true;
        }

        void _prepareTask()
        {
            block = true;
            eReplayGenerateResult = EReplayGenerateResult.Success;
            match = null;
            demoFilePath = Path.Combine(ClientParams.DEMO_DIR, string.Format("{0}.dem", match_id));
            replayFilePath = ClientParams.REPLAY_DIR + "/" + string.Format("{0}_{1}.mp4", match_id, account_id);
            replayResultFilePath = Path.Combine(ClientParams.REPLAY_DIR, string.Format("{0}_{1}.txt", match_id, account_id));
            //factory task
            mDFactories = new IMDFactory[4] {
                MDDotaClientRequestor.Instance,
                MDReplayDownloader.Instance,
                MDDemoAnalystor.Instance,
                MDMovieMaker.Instance,
            };
            File.WriteAllText(replayResultFilePath, EReplayGenerateResult.NotComplet.ToString());
        }

        EReplayGenerateResult _GetResult()
        {
            if (_downloadTask == null) return EReplayGenerateResult.NoTask;

            if (!_downloadTask.IsCompleted) return EReplayGenerateResult.NotComplet;

            return _downloadTask.Result;

        }

        void _Wait()
        {
            if (_downloadTask != null && !_downloadTask.IsCompleted)
            {
                _downloadTask.Wait();
            }
        }

        async Task<EReplayGenerateResult> _GenerateMatchReplayTask()
        {

            if (!MDFile.FileExists(replayFilePath))
            {
                for (int i = 0; i < mDFactories.Length; i++)
                {
                    mDFactories[i].Add(this);
                    while (block)
                    {
                        await Task.Delay(2000);
                    }
                    if (eReplayGenerateResult != EReplayGenerateResult.Success)
                    {
                        break;
                    }
                }
            }



            //create result file
            string result = eReplayGenerateResult.ToString();
            if (eReplayGenerateResult == EReplayGenerateResult.Success)
            {
                result += $"\n{replayFilePath}";
            }
            File.WriteAllText(replayResultFilePath, result);

            return eReplayGenerateResult;

        }

        private bool IsGenerateIdle()
        {
            if (_downloadTask == null) return true;
            return _downloadTask.IsCompleted;
        }

        public static void Generate(string request, bool anync = false)
        {
            MDReplayGenerator generator;
            if (!sMDReplayGeneratorMap.TryGetValue(request, out generator))
            {
                generator = new MDReplayGenerator(request);
                generator._Generate();
            }
            if (!anync)
                generator._Wait();
        }


        public static EReplayGenerateResult GetResult(string request)
        {
            MDReplayGenerator generator;
            if (!sMDReplayGeneratorMap.TryGetValue(request, out generator))
            {
                return EReplayGenerateResult.NoTask;
            }
            return generator._GetResult();
        }
    }
}

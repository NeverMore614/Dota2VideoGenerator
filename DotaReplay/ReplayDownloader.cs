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
    public class ReplayDownloader : SingleTon<ReplayDownloader>
    {
        private DotaClient _client;
        private string dotaLauncherPath = "game/bin/win64/dota2.exe";
        private string dotaMoviePath = "../movie";
        private ulong match_id = 0;
        private ulong account_id = 0;

        private Task _downloadTask;
        public ReplayDownloader()
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

        void _Download(string request)
        {
            if (string.IsNullOrEmpty(request)) return;

            if (!_client.IsLogonDota)
            {
                Console.WriteLine("未能链接到steam网络，退出");
                //_client.Reconnect();
                //await _Download(match_id);
            }

            if (!IsDownloadingIdle())
                return;
            else
            {
                //paris request content
                string[] splitArray = request.Split("_");
                try
                {
                    match_id = ulong.Parse(splitArray[0]);
                    account_id = ulong.Parse(splitArray[1]);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}");
                    return;
                }

                _client.RequestMatch(match_id);
                _client.WaitMatch();
                await DownLoadReplay(_client.Match);
            }
        }



        async Task<CMsgDOTAMatch> _GetMatch(ulong match_id)
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

        async static Task DownLoadReplay(CMsgDOTAMatch match)
        {
            if (match == null)
            {
                Console.WriteLine("No match details to display");
                return;
            }
            if (match.replay_state != CMsgDOTAMatch.ReplayState.REPLAY_AVAILABLE)
            {
                Console.WriteLine("录像不可用:" + match.replay_state);
                return;
            }

            var cluster = match.cluster;
            var match_id = match.match_id;
            var replay_salt = match.replay_salt;
            var _download_url = string.Format(ClientParams.DEMO_URL_STRING, cluster, match_id, replay_salt);
            Console.WriteLine("录像下载地址:" + _download_url);
            var save = string.Format(@"C:\Users\admin\Desktop\{0}.dem.bz2", match_id);
            if (!File.Exists(save))
            {
                Console.WriteLine("文件不存在，开始下载...");
                //先下载到临时文件
                var tmp = save + ".tmp";
                using (var web = new WebClient())
                {
                    await web.DownloadFileTaskAsync(_download_url, tmp);
                }
                File.Move(tmp, save, true);
                Console.WriteLine("文件下载成功");
            }
            Console.WriteLine("下载完毕");

        }

        public static CMsgDOTAMatch GetMatch(ulong match_id)
        {
           Task<CMsgDOTAMatch> task = Instance._GetMatch(match_id);
            task.Wait();
           return task.Result;
        }


        public static bool IsDownloadingIdle()
        {
            if (Instance._downloadTask == null) return true;
            return Instance._downloadTask.IsCompleted;
        }

        public static void Download(string request)
        {
            Instance._Download(request);
        }
    }
}

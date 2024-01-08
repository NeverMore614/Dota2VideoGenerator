using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SteamKit2.GC.Dota.Internal;
using static SteamKit2.Internal.CMsgDownloadRateStatistics;

namespace MetaDota.DotaReplay
{
    class MDReplayDownloader : MDFactory<MDReplayDownloader>
    {

        public override async Task Work(MDReplayGenerator mDReplayGenerator)
        {
            string savePath = Path.Combine(ClientParams.DEMO_DIR, $"{mDReplayGenerator.match_id}.dem");
            if (mDReplayGenerator.match == null)
            {
                mDReplayGenerator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.NoMatch;
            }
            else if (mDReplayGenerator.match.replay_state != CMsgDOTAMatch.ReplayState.REPLAY_AVAILABLE)
            {
                mDReplayGenerator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DemoUnavailable;
            }
            else if (File.Exists(savePath))
            {
                return;
            }

            var cluster = match.cluster;
            var match_id = match.match_id;
            var replay_salt = match.replay_salt;
            var _download_url = string.Format(ClientParams.DEMO_URL_STRING, cluster, match_id, replay_salt);
            Console.WriteLine("demo url:" + _download_url);
            var zip = string.Format(savePath + ".bz2", match_id);
            if (!File.Exists(zip))
            {
                Console.WriteLine(zip + " downloading...");
                //先下载到临时文件
                var tmp = zip + ".tmp";
                using (var web = new WebClient())
                {
                    web.DownloadFileTaskAsync(_download_url, tmp).Wait();
                }

                File.Move(tmp, zip, true);
                Console.WriteLine("demo download success");
            }
            //start unzip demo
            using (Process zipProcess = new Process())
            {
                zipProcess.StartInfo.FileName = "7z.exe";
                zipProcess.StartInfo.RedirectStandardInput = true;
                zipProcess.StartInfo.Arguments = $"x {zip} -o{ClientParams.DEMO_DIR} -aoa";
                zipProcess.Start();
                zipProcess.WaitForExit();
            }
            File.Delete(zip);
            Console.WriteLine("download complete");
        }
    }
}

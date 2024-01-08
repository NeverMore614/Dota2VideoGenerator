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
    internal class MDReplayDownloader
    {

        static Queue<Task> _task_queue;
        public async static Task Init()
        {
            _task_queue = new Queue<Task>();
            while (true)
            {
                await Task.Delay(5000);
                if (_task_queue.Count > 0)
                {
                    Task task = _task_queue.Dequeue();
                    task.Start();
                    await task;
                }
            }
        }
        public static Task _DownLoadReplay(CMsgDOTAMatch match, string savePath)
        {
            Task downloadTask = new Task(() => _DownLoadReplayAction(match, savePath));

            _task_queue.Enqueue(downloadTask);

            return downloadTask;
        }

        static void _DownLoadReplayAction(CMsgDOTAMatch match, string savePath)
        {
            if (match == null)
            {
                Console.WriteLine("No match details to display");
                return;
            }
            if (match.replay_state != CMsgDOTAMatch.ReplayState.REPLAY_AVAILABLE)
            {
                Console.WriteLine("demo unavailable:" + match.replay_state);
                return;
            }
            if (File.Exists(savePath))
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

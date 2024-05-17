using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDota.DotaReplay
{
    internal class ClientParams
    {
        public const string DEMO_URL_STRING = "http://replay{0}.valve.net/570/{1}_{2}.dem.bz2";
        public const int APPID = 570;
        public const string STEAM_USERNAME = "13990092481";
        public const string STEAM_PASSWORD = "8111528yui";
        public const int DOWNLOAD_CHECK_INTERVAL = 10000;
        public const string MATCH_REQUEST_FILE = "matchRequest.txt";
        public const string REPLAY_DIR = "replays";
        public const string DEMO_DIR = "demos";
        public const string REPLAY_CFG_DIR = "replayCfg";
        public const string CONFIG_DIR = "config";

        public const string WEB_INVALID_REQUEST = "your request is invalid :";
    }
}

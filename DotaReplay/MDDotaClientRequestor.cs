using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MetaDota.DotaReplay
{
    internal class MDDotaClientRequestor : MDFactory<MDDotaClientRequestor>
    {
        public override async Task Work(MDReplayGenerator generator)
        {
            DotaClient _client = DotaClient.Instance;
            if (!_client.IsLogonDota)
            {
                Console.WriteLine("未能链接到steam网络，退出");
                generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DisConnectServer;
            }
            else
            {
                _client.RequestMatch(generator.match_id);
                _client.WaitMatch();
                if (_client.Match == null) {
                    generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.NoMatch;
                }
                generator.match = _client.Match;
            }
            generator.block = false;
        }
    }
}

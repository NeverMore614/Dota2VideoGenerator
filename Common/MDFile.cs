using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using MetaDota.DotaReplay;

namespace MetaDota.Common
{
    internal class MDFile
    {
        public static void Init()
        { 
            if (!Directory.Exists(ClientParams.DEMO_DIR))
            {
                Directory.CreateDirectory(ClientParams.DEMO_DIR);
            }

            if (!Directory.Exists(ClientParams.REPLAY_DIR))
            {
                Directory.CreateDirectory(ClientParams.REPLAY_DIR);
            }
            if (!Directory.Exists(ClientParams.REPLAY_CFG_DIR))
            {
                Directory.CreateDirectory(ClientParams.REPLAY_CFG_DIR);
            }
        }
        public static string ReadLine(string path, ref string result)
        {
            try
            {
                result = File.ReadLines(path).First();
            }
            catch (Exception e)
            {
                
                Console.WriteLine(e);
            }
            return result;
        }

       

        public static bool FileExists(string path)
        { 
            return File.Exists(path);
        }
    }
}

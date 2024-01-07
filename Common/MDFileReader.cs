using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MetaDota.Common
{
    internal class MDFileReader
    {
        public static string ReadLine(string path, ref string result)
        {
            try
            {
                result = File.ReadLines(path).First();
            }
            catch (Exception e)
            {
                result = "";
                Console.WriteLine(e);
            }
            return result;
        }
    }
}

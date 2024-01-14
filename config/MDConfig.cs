using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDota.config
{
    internal class MDConfig
    {
        public string dotaPath = "";
        public string steamAccount = "";
        public string steamPassword = "";
        public string serverIp = "";
        public string serverPort = "";

        public MDConfig() {
            string value = "";
            string fieldPath = "";
            foreach (var field in typeof(MDConfig).GetFields())
            {
                value = "";
                fieldPath = $"config/{field.Name}.txt";
                if (File.Exists(fieldPath))
                {
                    value = File.ReadAllText(fieldPath);
                }
                while (value == "")
                {
                    Console.WriteLine($"please enter your {field.Name}");
                    value = Console.ReadLine();
                    File.WriteAllText(fieldPath, value);
                }
                field.SetValue(this, value);
            }
        }
    }
}

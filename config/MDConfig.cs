using Newtonsoft.Json.Linq;
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
        public string webServerUrl = "";

        private int _keyInputDelay = 500;
        private string _authGuardData = null;
        private string _bearerAuth = null;
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
                    if (field.Name == "steamPassword")
                    {
                        value = MDTools.ReadPassword();
                    }
                    else
                    {
                        value = Console.ReadLine();
                    }
                    File.WriteAllText(fieldPath, value);
                }
                field.SetValue(this, value);
            }
            fieldPath = $"config/authGuardData.txt";
            if (File.Exists(fieldPath))
            {
                _authGuardData = File.ReadAllText(fieldPath);
                if (string.IsNullOrEmpty(_authGuardData))
                    _authGuardData = null;
            }
            fieldPath = $"config/bearer.txt";
            if (File.Exists(fieldPath))
            {
                _bearerAuth = File.ReadAllText(fieldPath);
                if (string.IsNullOrEmpty(_bearerAuth))
                    _bearerAuth = null;
            }

            fieldPath = $"config/keyInputDelay.txt";
            if (File.Exists(fieldPath))
            {
                try {
                    string delay = File.ReadAllText(fieldPath);
                    if (!string.IsNullOrEmpty(_authGuardData))
                        _keyInputDelay = int.Parse(delay);
                }
                catch (Exception ex) { }

            }
        }

        public int GetKeyInputDelay()
        { return _keyInputDelay; }

        public string GetAuthGuardData()
        {
            return _authGuardData;
        }

        public string GetBearer()
        {
            return _bearerAuth;
        }

        public void SaveAuthGuardData(string data)
        {
            _authGuardData = data;
            string fieldPath = $"config/authGuardData.txt";
            File.WriteAllText(fieldPath, _authGuardData);
        }
    }
}

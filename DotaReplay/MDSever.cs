using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ConsoleApp2;
using SteamKit2.CDN;

namespace MetaDota.DotaReplay
{
    internal class MDSever : SingleTon<MDSever>
    {
        private static int _port = 8885;
        private static byte[] _result = new byte[1024];
        private static Socket _socket;

        public void Start()
        {
            if (!File.Exists("config/ipConfig.txt"))
            {
                File.Create("config/ipConfig.txt");
            }
            string ipConfig = File.ReadAllText("config/ipConfig.txt");
            if (string.IsNullOrEmpty(ipConfig))
            {
                Console.Write("please input your ip address(xxx.xxx.xxx.xxx:port):");
                ipConfig = Console.ReadLine();
            }
            string[] ips = ipConfig.Split(":");
            if (ips.Length != 2)
            {
                Console.Write("wroing ipconfig:" + ipConfig);
            }
            File.WriteAllText("config/ipConfig.txt", ipConfig);

            IPAddress ip = IPAddress.Parse(ips[0]);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(ip, int.Parse(ips[1])));
            _socket.Listen(10);
            Console.WriteLine("meta dota server start");
            Thread thread = new Thread(ListenClientConnect);
            thread.Start();
        }


        private static void ListenClientConnect()
        {
            while (true)
            {
                Socket clientSocket = _socket.Accept();
                Thread thread = new Thread(Working);
                thread.Start(clientSocket);
            }
        }

        static void Working(object client)
        {
            Socket clientSocket = client as Socket;
            while (clientSocket.Connected)
            {
                
                int bytes = clientSocket.Receive(_result);
                string receivedData = Encoding.ASCII.GetString(_result, 0, bytes);
                Console.WriteLine("received from client :" + receivedData);
                Program.requestQueue.Enqueue(receivedData);
            }
        }
    }


}

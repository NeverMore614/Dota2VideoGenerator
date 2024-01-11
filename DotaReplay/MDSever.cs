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
        class SocketClient
        {
            public Socket Socket;
            public bool Heartbeat = false;
            public byte[] ReceiveBytes;
            public Thread Connect;
            public bool shouldStop = false;
            public SocketClient()
            {
                Heartbeat = false;
                ReceiveBytes = new byte[1024];
            }

            public void Accept(Socket socket)
            { 
            
            }

            public void Close()
            {
                Console.WriteLine("close connect");
                Heartbeat = false;
                shouldStop = true;
                Connect.Join();
                Socket.Close();
                Socket = null;
                Console.WriteLine("close connect over");
            }

            public bool IsIdle()
            {
                return Socket == null;
            }
        }

        private SocketClient[] socketClients;

        private Socket _socket;

        public MDSever()
        {
            socketClients = new SocketClient[10];
            for (int i = 0; i < socketClients.Length; i++)
            {
                socketClients[i] = new SocketClient();
            }

            Thread thread = new Thread(HeartbeatCheck);
            thread.Start();
        }
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


        private void ListenClientConnect()
        {
            while (true)
            {
                Socket clientSocket = _socket.Accept();
                bool save = false;
                for (int i = 0; i < socketClients.Length; i++)
                {
                    if (socketClients[i].IsIdle())
                    {
                        socketClients[i].Accept(clientSocket);
                        save = true;
                        break;
                    }
                }
                if (!save)
                {
                    clientSocket.Close();
                }
                   

            }
        }

        static void Working(object o)
        {
            SocketClient socketClient = o as SocketClient;
            Socket socket = socketClient.Socket;
            while (!socketClient.shouldStop)
            {
                int bytes = socket.Receive(socketClient.ReceiveBytes);
                if (bytes > 0)
                {
                    string receivedData = Encoding.ASCII.GetString(socketClient.ReceiveBytes, 0, bytes);
                    Console.WriteLine("received from client :" + receivedData);
                    Program.requestQueue.Enqueue(receivedData);
                }

            }
        }

        void HeartbeatCheck()
        {
            while (true)
            {
                Thread.Sleep(10000);
                for (int i = 0; i < socketClients.Length; i++)
                {
                    SocketClient socketClient = socketClients[i];
                    if (socketClient.Socket != null)
                    {
                        if (socketClient.Heartbeat)
                        {
                            socketClient.Heartbeat = false;
                        }
                        else
                        {
                            socketClient.Close();
                    
                        }
   
                    }
                }
            }
        }
    }


}

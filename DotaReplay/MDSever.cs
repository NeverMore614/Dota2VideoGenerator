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
            public int Heartbeat = 0;
            public byte[] ReceiveBytes;
            public Thread Connect;
            public bool shouldStop = false;
            public SocketClient()
            {
                ReceiveBytes = new byte[1024];
            }

            public void Accept(Socket socket)
            {
                Socket = socket;
                Thread thread = new Thread(Working);
                thread.Start(this);
                Connect = thread;
                shouldStop = false;
                Heartbeat = 10;
            }

            public void Close()
            {
                Console.WriteLine("close connect");
                shouldStop = true;
                Socket.Close();
                Connect.Join();
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

        private IPAddress _ip;

        public MDSever()
        {
            socketClients = new SocketClient[10];
            for (int i = 0; i < socketClients.Length; i++)
            {
                socketClients[i] = new SocketClient();
            }

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress iPAddress in host.AddressList)
            {
                Console.WriteLine($"{iPAddress}");
                if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
                { 
                    _ip = iPAddress;
                }
            }

        }
        public void Start()
        {
            if (!File.Exists("config/ipConfig.txt"))
            {
                File.Create("config/ipConfig.txt");
            }
            string port = File.ReadAllText("config/serverPort.txt");
            if (string.IsNullOrEmpty(port))
            {
                Console.Write("please input your port number:");
                port = Console.ReadLine();
            }
            File.WriteAllText("config/serverPort.txt", port);
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Bind(new IPEndPoint(_ip, int.Parse(port)));
                _socket.Listen(10);
                Console.WriteLine("meta dota server start success, ur ipaddress is:" + _ip.ToString() + ":" + port);

                Thread thread = new Thread(ListenClientConnect);
                thread.Start();

                Thread thread1 = new Thread(HeartbeatCheck);
                thread1.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("meta dota server start fail " + ex.ToString());
            }
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
                try
                {
                    int bytes = socket.Receive(socketClient.ReceiveBytes);
                    if (bytes > 0)
                    {
                        string receivedData = Encoding.ASCII.GetString(socketClient.ReceiveBytes, 0, bytes);
          
                        string[] interfaceAndParam = receivedData.Split('/');
                        if (interfaceAndParam.Length == 2)
                        {
                            switch (interfaceAndParam[0])
                            {
                                case "heart":
                                    socketClient.Heartbeat = 10;
                                    break;
                                case "match":
                                    Program.requestQueue.Enqueue(interfaceAndParam[1]);
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("invalid request :" + receivedData);
                        }
                    }
                }
                catch
                {
                    socketClient.shouldStop = true;
                }
            }
        }




        void HeartbeatCheck()
        {
            while (true)
            {
                Thread.Sleep(1000);
                for (int i = 0; i < socketClients.Length; i++)
                {
                    SocketClient socketClient = socketClients[i];
                    if (socketClient.Socket != null)
                    {
                        if (0 > --socketClient.Heartbeat )
                        {
                            socketClient.Close();
                        }
   
                    }
                }
            }
        }
    }


}

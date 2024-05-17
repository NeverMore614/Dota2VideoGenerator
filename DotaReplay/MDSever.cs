
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
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
            public Thread Connect;
            public SocketClient()
            {

            }

            public void Accept(Socket socket)
            {
                Socket = socket;
                Thread thread = new Thread(Working);
                thread.Start(this);
                Connect = thread;
                Heartbeat = 10;
            }

            public void Close()
            {
                Console.WriteLine("close connect");
                Socket.Close();
                Connect.Join();
                Console.WriteLine("close connect over");
            }

            public bool IsIdle()
            {
                return Connect == null || !Connect.IsAlive;
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

            try
            {
                _ip = IPAddress.Parse(Program.config.serverIp);
            }
            catch
            {
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



        }
        public void Start()
        {
            string port = Program.config.serverPort;
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

            if (Process.GetProcessesByName("hfs").Length == 0)
            {
                Process process = new Process();
                process.StartInfo.FileName = "hfs.exe";
                process.StartInfo.Arguments = Path.GetFullPath("replays");
                process.Start();
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
            byte[] ReceiveBytes = new byte[1024];
            string result = "";
            try
            {
                int bytes = socket.Receive(ReceiveBytes);
                if (bytes > 0)
                {
                    string receivedData = Encoding.ASCII.GetString(ReceiveBytes, 0, bytes);
                    string[] interfaceAndParam = receivedData.Split('/');
                    if (interfaceAndParam.Length == 2)
                    {
                        switch (interfaceAndParam[0])
                        {
                            case "match":
                                bool match = false;
                                foreach (string s in Program.requestQueue)
                                {
                                    if (s.Equals(interfaceAndParam[1]))
                                    {
                                        match = true;
                                        break;

                                    }
                                }
                                if (match)
                                {
                                    result = "Has Task";

                                }
                                else
                                {
                                    Program.requestQueue.Enqueue(interfaceAndParam[1]);
                                    result = "OK";
                                }
                                break;
                            case "replay":
                                string resultFilePath = Path.Combine(ClientParams.REPLAY_DIR, interfaceAndParam[1] + ".txt");
                                if (File.Exists(resultFilePath))
                                {
                                    string[] lines = File.ReadAllLines(resultFilePath);
                                    result = lines[0] + "$" + (lines.Length > 1 ? lines[1] : "");
                                }
                                else
                                {
                                    result = "None";
                                }
                                break;
                        }
                    }
                    else
                    {
                        result = "invalid request :" + receivedData;
                        Console.WriteLine("invalid request :" + receivedData);
                    }
                }
            }
            catch
            {
                result = "Error";
            }
            socket.Send(Encoding.ASCII.GetBytes(result));
            socket.Close();
        }




        void HeartbeatCheck()
        {
            while (true)
            {
                Thread.Sleep(1000);
                for (int i = 0; i < socketClients.Length; i++)
                {
                    SocketClient socketClient = socketClients[i];
                    if (socketClient.Connect != null && socketClient.Connect.IsAlive)
                    {
                        if (0 > --socketClient.Heartbeat)
                        {
                            socketClient.Close();
                        }
                    }
                }
            }
        }
    }


}
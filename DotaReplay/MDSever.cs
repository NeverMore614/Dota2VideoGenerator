using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ConsoleApp2;

namespace MetaDota.DotaReplay
{
    internal class MDSever : SingleTon<MDSever>
    {
        private static int _port = 8885;
        private static byte[] _result = new byte[1024];
        private static Socket _socket;

        public void Start()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(ip, _port));
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
                int bytes = clientSocket.Receive(_result);
                string receivedData = Encoding.ASCII.GetString(_result, 0, bytes);
                Console.WriteLine("received from client :" + receivedData);
                Program.requestQueue.Enqueue(receivedData);
            }
        }
    }


}

using System;
using System.Net.Sockets;
using System.Net;

namespace NetworkServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IPHostEntry ipHost = Dns.GetHostEntry("");
            IPAddress ipAddress = ipHost.AddressList[1];
            Console.WriteLine("IP адрес сервера: " + ipAddress);
            TcpListener listener = new TcpListener(ipAddress, 8888);
            Server server = new Server(listener);
            server.Start();
        }
    }
}
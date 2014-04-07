using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DriftPlayer
{
    static class TcpServer
    {
        private static readonly TcpListener listener;
        private static readonly List<TcpClient> clients;
        private static readonly object locker;

        static TcpServer()
        {
            TcpServer.locker = new object();
            TcpServer.listener = new TcpListener(IPAddress.Any, 23232);
            listener.ExclusiveAddressUse = false;
            TcpServer.clients = new List<TcpClient>();
        }

        public static void Start()
        {
            TcpServer.listener.Start();
            TcpServer.ListenForClients();
        }

        private static Task ListenForClients()
        {
            return Task.Run(() =>
            {
                TcpServer.listener.Start();
                while (true)
                {
                    if (TcpServer.listener.Pending())
                    {
                        TcpClient client = TcpServer.listener.AcceptTcpClient();
                        Console.WriteLine("Client connected.");
                        TcpServer.AddClient(client);
                    }
                    Thread.Sleep(100);
                }
            });
        }

        public static void Write(byte[] buffer, int offset, int count)
        {
            Console.WriteLine("Write");
            lock (TcpServer.locker)
            {
                foreach (TcpClient client in TcpServer.clients)
                {
                    client.Client.Send(buffer, offset, count, SocketFlags.None);
                }
            }
        }

        private static void AddClient(TcpClient client)
        {
            lock (TcpServer.locker)
            {
                TcpServer.clients.Add(client);
            }
        }

        private static void RemoveClient(TcpClient client)
        {
            lock (TcpServer.locker)
            {
                TcpServer.clients.Remove(client);
            }
        }

        public static void Stop()
        {
            TcpServer.listener.Stop();
        }
    }
}

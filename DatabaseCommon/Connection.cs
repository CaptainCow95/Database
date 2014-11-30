using System;
using System.Net.Sockets;

namespace Database.Common
{
    public class Connection
    {
        public Connection(TcpClient client, DateTime lastActiveTime, ConnectionStatus status)
        {
            Client = client;
            LastActiveTime = lastActiveTime;
            Status = status;
        }

        public TcpClient Client { get; private set; }

        public DateTime LastActiveTime { get; set; }

        public ConnectionStatus Status { get; set; }
    }
}
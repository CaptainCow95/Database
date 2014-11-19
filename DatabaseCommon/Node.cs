using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Database.Common
{
    public abstract class Node
    {
        private const int ConnectionTimeout = 30;
        private readonly Dictionary<string, Connection> _connections = new Dictionary<string, Connection>();
        private readonly Dictionary<string, StringBuilder> _messagesReceived = new Dictionary<string, StringBuilder>();
        private readonly int _port;
        private Thread _connectionCleanerThread;
        private TcpListener _connectionListener;
        private Thread _connectionListenerThread;
        private Thread _messageListenerThread;
        private Thread _messageSenderThread;
        private bool _running = false;

        protected Node(int port)
        {
            _port = port;
        }

        protected void AfterStop()
        {
            if (_running)
            {
                _running = false;

                _connectionListener.Stop();
            }
        }

        protected void BeforeStart()
        {
            _running = true;

            _messageListenerThread = new Thread(RunMessageListener);
            _messageListenerThread.Start();

            _messageSenderThread = new Thread(RunMessageSender);
            _messageSenderThread.Start();

            _connectionListenerThread = new Thread(RunConnectionListener);
            _connectionListenerThread.Start();

            _connectionCleanerThread = new Thread(RunConnectionCleaner);
            _connectionCleanerThread.Start();
        }

        private void RunConnectionCleaner()
        {
            for (int i = 0; i < ConnectionTimeout; ++i)
            {
                Thread.Sleep(1000);

                if (!_running)
                {
                    break;
                }
            }

            while (_running)
            {
                // MAKE SURE THIS LOCK ORDER IS THE SAME EVERYWHERE
                lock (_connections) lock (_messagesReceived)
                    {
                        DateTime now = DateTime.UtcNow;
                        var toRemove = new List<string>();
                        foreach (var item in _connections)
                        {
                            if (!item.Value.Client.Connected || (now - item.Value.LastActiveTime).TotalSeconds > ConnectionTimeout)
                            {
                                toRemove.Add(item.Key);
                            }
                        }

                        foreach (var item in toRemove)
                        {
                            _connections[item].Client.Close();
                            _connections.Remove(item);
                            _messagesReceived.Remove(item);
                        }
                    }

                for (int i = 0; i < 5; ++i)
                {
                    Thread.Sleep(1000);

                    if (!_running)
                    {
                        break;
                    }
                }
            }
        }

        private void RunConnectionListener()
        {
            _connectionListener = new TcpListener(IPAddress.Any, _port);
            _connectionListener.Start();
            while (_running)
            {
                var incoming = _connectionListener.AcceptTcpClient();
                lock (_connections)
                {
                    _connections.Add(incoming.Client.RemoteEndPoint.ToString(),
                        new Connection(incoming, DateTime.Now));
                }
            }
        }

        private void RunMessageListener()
        {
            const int messageBufferSize = 1024;
            var messageBuffer = new byte[messageBufferSize];
            while (_running)
            {
                // MAKE SURE THIS LOCK ORDER IS THE SAME EVERYWHERE
                lock (_connections) lock (_messagesReceived)
                    {
                        foreach (var connection in _connections)
                        {
                            if (!connection.Value.Client.Connected)
                            {
                                continue;
                            }

                            if (!_messagesReceived.ContainsKey(connection.Key))
                            {
                                _messagesReceived.Add(connection.Key, new StringBuilder());
                            }

                            NetworkStream stream = connection.Value.Client.GetStream();
                            if (stream.DataAvailable)
                            {
                                int bytesRead = stream.Read(messageBuffer, 0, messageBufferSize);
                                _messagesReceived[connection.Key].Append(Encoding.Default.GetString(messageBuffer, 0,
                                    bytesRead));
                                connection.Value.LastActiveTime = DateTime.Now;
                            }
                        }
                    }

                lock (_messagesReceived)
                {
                    // Check for complete messages.
                }

                Thread.Sleep(1);
            }
        }

        private void RunMessageSender()
        {
        }

        private class Connection
        {
            public Connection(TcpClient client, DateTime lastActiveTime)
            {
                Client = client;
                LastActiveTime = lastActiveTime;
            }

            public TcpClient Client { get; private set; }

            public DateTime LastActiveTime { get; set; }
        }
    }
}
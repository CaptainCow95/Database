using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<string, List<byte>> _messagesReceived = new Dictionary<string, List<byte>>();
        private readonly Queue<byte[]> _messagesToSend = new Queue<byte[]>();
        private readonly int _port;
        private Thread _connectionCleanerThread;
        private TcpListener _connectionListener;
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

            _connectionListener = new TcpListener(IPAddress.Any, _port);
            _connectionListener.Start();
            _connectionListener.BeginAcceptTcpClient(ProcessConnectionRequest, null);

            _connectionCleanerThread = new Thread(RunConnectionCleaner);
            _connectionCleanerThread.Start();
        }

        private void ProcessConnectionRequest(IAsyncResult result)
        {
            TcpClient incoming;
            try
            {
                incoming = _connectionListener.EndAcceptTcpClient(result);
                _connectionListener.BeginAcceptTcpClient(ProcessConnectionRequest, null);
            }
            catch (ObjectDisposedException)
            {
                // The connection listener was shutdown, stop the async loop.
                return;
            }

            lock (_connections)
            {
                _connections.Add(incoming.Client.RemoteEndPoint.ToString(), new Connection(incoming, DateTime.Now));
            }
        }

        private void ProcessMessage(byte[] message)
        {
            Console.WriteLine(Encoding.Default.GetString(message));
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
                                _messagesReceived.Add(connection.Key, new List<byte>());
                            }

                            NetworkStream stream = connection.Value.Client.GetStream();
                            if (stream.DataAvailable)
                            {
                                int bytesRead = stream.Read(messageBuffer, 0, messageBufferSize);
                                _messagesReceived[connection.Key].AddRange(messageBuffer.Take(bytesRead));
                                connection.Value.LastActiveTime = DateTime.Now;
                            }
                        }
                    }

                List<byte[]> messages = new List<byte[]>();
                lock (_messagesReceived)
                {
                    foreach (var message in _messagesReceived)
                    {
                        if (message.Value.Count >= 4)
                        {
                            int length = BitConverter.ToInt32(message.Value.Take(4).ToArray(), 0);
                            if (message.Value.Count > length + 4)
                            {
                                messages.Add(message.Value.Skip(4).Take(length).ToArray());
                            }
                        }
                    }
                }

                foreach (var message in messages)
                {
                    ProcessMessage(message);
                }

                Thread.Sleep(1);
            }
        }

        private void RunMessageSender()
        {
            while (_running)
            {
                lock (_messagesToSend)
                {
                    while (_messagesToSend.Count > 0)
                    {
                        var message = _messagesToSend.Dequeue();
                        ThreadPool.QueueUserWorkItem(SendMessageInternal, message);
                    }
                }

                Thread.Sleep(1);
            }
        }

        private void SendMessageInternal(object data)
        {
            var message = (Message)data;

            lock (_connections)
            {
                if (_connections.ContainsKey(message.Address))
                {
                    var stream = _connections[message.Address].Client.GetStream();
                    stream.Write(message.Data, 0, message.Data.Length);
                }
            }
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
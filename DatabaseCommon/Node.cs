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
        private const int ResponseTimeout = 30;
        private readonly Dictionary<string, Connection> _connections = new Dictionary<string, Connection>();
        private readonly Dictionary<string, List<byte>> _messagesReceived = new Dictionary<string, List<byte>>();
        private readonly Queue<Message> _messagesToSend = new Queue<Message>();
        private readonly int _port;
        private readonly Dictionary<uint, Tuple<Message, DateTime>> _waitingForResponses = new Dictionary<uint, Tuple<Message, DateTime>>();
        private Thread _cleanerThread;
        private TcpListener _connectionListener;
        private Thread _messageListenerThread;
        private Thread _messageSenderThread;
        private bool _running = false;

        protected Node(int port)
        {
            _port = port;
        }

        public bool Running { get { return _running; } }

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

            _cleanerThread = new Thread(RunCleaner);
            _cleanerThread.Start();
        }

        protected void SendMessage(Message message)
        {
            message.Status = MessageStatus.Sending;
            lock (_messagesToSend)
            {
                _messagesToSend.Enqueue(message);
            }
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

            Connection connection = new Connection(incoming, DateTime.Now, ConnectionStatus.ConfirmingConnection);
            lock (_connections)
            {
                _connections.Add(incoming.Client.RemoteEndPoint.ToString(), connection);
            }

            // TODO: Confirm Connection

            connection.Status = ConnectionStatus.Connected;
        }

        private void ProcessMessage(string address, byte[] data)
        {
            Message message = new Message(address, data);
            if (message.InResponseTo != 0)
            {
                lock (_waitingForResponses)
                {
                    if (_waitingForResponses.ContainsKey(message.InResponseTo))
                    {
                        Message waiting = _waitingForResponses[message.InResponseTo].Item1;
                        waiting.Response = message;
                        waiting.Status = MessageStatus.ResponseReceived;
                        _waitingForResponses.Remove(message.InResponseTo);
                    }
                }
            }
            Console.WriteLine(Encoding.Default.GetString(data));
        }

        private void RunCleaner()
        {
            while (_running)
            {
                DateTime now = DateTime.UtcNow;
                List<uint> responsesToRemove = new List<uint>();
                lock (_waitingForResponses)
                {
                    foreach (var item in _waitingForResponses)
                    {
                        if ((now - item.Value.Item2).TotalSeconds > ResponseTimeout)
                        {
                            item.Value.Item1.Status = MessageStatus.ResponseTimeout;
                            responsesToRemove.Add(item.Key);
                        }
                    }

                    foreach (var item in responsesToRemove)
                    {
                        _waitingForResponses.Remove(item);
                    }
                }

                List<string> connectionsToRemove = new List<string>();
                lock (_connections)
                {
                    foreach (var connection in _connections)
                    {
                        if (!connection.Value.Client.Connected ||
                            (now - connection.Value.LastActiveTime).TotalSeconds > ConnectionTimeout)
                        {
                            connectionsToRemove.Add(connection.Key);
                        }
                    }

                    foreach (var connection in connectionsToRemove)
                    {
                        _connections.Remove(connection);
                    }
                }

                // Keep thread responsive to shutdown while waiting for next run (5 seconds).
                int i = 0;
                while (_running && i < 5 * 4)
                {
                    Thread.Sleep(250);
                    ++i;
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
                            while (stream.DataAvailable)
                            {
                                int bytesRead = stream.Read(messageBuffer, 0, messageBufferSize);
                                _messagesReceived[connection.Key].AddRange(messageBuffer.Take(bytesRead));
                                connection.Value.LastActiveTime = DateTime.Now;
                            }
                        }
                    }

                List<Tuple<string, byte[]>> messages = new List<Tuple<string, byte[]>>();
                lock (_messagesReceived)
                {
                    foreach (var message in _messagesReceived)
                    {
                        if (message.Value.Count >= 4)
                        {
                            int length = BitConverter.ToInt32(message.Value.Take(4).ToArray(), 0);
                            if (message.Value.Count > length + 4)
                            {
                                messages.Add(new Tuple<string, byte[]>(message.Key, message.Value.Skip(4).Take(length).ToArray()));
                            }
                        }
                    }
                }

                foreach (var message in messages)
                {
                    ProcessMessage(message.Item1, message.Item2);
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

                        lock (_connections)
                        {
                            if (_connections.ContainsKey(message.Address) && (_connections[message.Address].Status == ConnectionStatus.Connected || message.SendWithoutConfirmation))
                            {
                                try
                                {
                                    if (message.WaitingForResponse)
                                    {
                                        lock (_waitingForResponses)
                                        {
                                            _waitingForResponses.Add(message.ID, new Tuple<Message, DateTime>(message, DateTime.UtcNow));
                                        }
                                    }

                                    byte[] dataToSend = message.EncodeMessage();
                                    var stream = _connections[message.Address].Client.GetStream();
                                    stream.Write(dataToSend, 0, dataToSend.Length);

                                    message.Status = message.WaitingForResponse ? MessageStatus.WaitingForResponse : MessageStatus.Sent;
                                }
                                catch (ObjectDisposedException)
                                {
                                    message.Status = MessageStatus.SendingFailure;

                                    lock (_waitingForResponses)
                                    {
                                        _waitingForResponses.Remove(message.ID);
                                    }
                                }
                            }
                            else
                            {
                                message.Status = MessageStatus.SendingFailure;
                            }
                        }
                    }
                }

                Thread.Sleep(1);
            }
        }
    }
}
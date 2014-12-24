using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Database.Common
{
    /// <summary>
    /// Represents a database node.
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// The amount of seconds before a connection causes a timeout.
        /// </summary>
        private const int ConnectionTimeout = 30;

        /// <summary>
        /// The size of the buffer, in bytes, to be read at a time by the message listener.
        /// </summary>
        private const int MessageBufferSize = 1024;

        /// <summary>
        /// The amount of seconds before waiting for a response causes a timeout.
        /// </summary>
        private const int ResponseTimeout = 30;

        /// <summary>
        /// A collection of the current connections.
        /// </summary>
        private readonly Dictionary<string, Connection> _connections = new Dictionary<string, Connection>();

        /// <summary>
        /// A collection of the current messages to be processed.
        /// </summary>
        private readonly Dictionary<string, List<byte>> _messagesReceived = new Dictionary<string, List<byte>>();

        /// <summary>
        /// A queue of the messages to be sent.
        /// </summary>
        private readonly Queue<Message> _messagesToSend = new Queue<Message>();

        /// <summary>
        /// The port the node is running on.
        /// </summary>
        private readonly int _port;

        private readonly NodeType _type;

        /// <summary>
        /// A collection of messages that are waiting for responses.
        /// </summary>
        private readonly Dictionary<uint, Tuple<Message, DateTime>> _waitingForResponses = new Dictionary<uint, Tuple<Message, DateTime>>();

        /// <summary>
        /// The thread dealing with various cleanup operations.
        /// </summary>
        private Thread _cleanerThread;

        private List<NodeDefinition> _connectionList;

        /// <summary>
        /// The thread listening for new collections.
        /// </summary>
        private TcpListener _connectionListener;

        private string _connectionString;

        /// <summary>
        /// The thread listening for new messages.
        /// </summary>
        private Thread _messageListenerThread;

        /// <summary>
        /// The thread sending messages.
        /// </summary>
        private Thread _messageSenderThread;

        /// <summary>
        /// A value indicating whether the node is running.
        /// </summary>
        private bool _running = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="port">The port to run the node on.</param>
        protected Node(NodeType type, int port, string connectionString, List<NodeDefinition> connectionList)
        {
            _type = type;
            _port = port;
            _connectionString = connectionString;
            _connectionList = connectionList;
        }

        public List<NodeDefinition> ConnectionList { get { return _connectionList; } }

        public string ConnectionString { get { return _connectionString; } }

        /// <summary>
        /// Gets a value indicating whether the node is running.
        /// </summary>
        public bool Running
        {
            get { return _running; }
        }

        protected Dictionary<string, Connection> Connections
        {
            get { return _connections; }
        }

        /// <summary>
        /// Runs the node.
        /// </summary>
        public abstract void Run();

        /// <summary>
        /// Called after the node has finished shutting down.
        /// </summary>
        protected void AfterStop()
        {
            if (_running)
            {
                _running = false;

                _connectionListener.Stop();
            }
        }

        /// <summary>
        /// Called before the node starts up.
        /// </summary>
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

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        protected void SendMessage(Message message)
        {
            message.Status = MessageStatus.Sending;
            lock (_messagesToSend)
            {
                _messagesToSend.Enqueue(message);
            }
        }

        /// <summary>
        /// Process a connection request.
        /// </summary>
        /// <param name="result">The result of the connection listener.</param>
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
        }

        /// <summary>
        /// Processes a message.
        /// </summary>
        /// <param name="address">The address the message was sent from.</param>
        /// <param name="data">The data to be decoded and processed.</param>
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

        /// <summary>
        /// The run method of the cleaner thread.
        /// </summary>
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

        /// <summary>
        /// The run method of the message listener thread.
        /// </summary>
        private void RunMessageListener()
        {
            var messageBuffer = new byte[MessageBufferSize];
            while (_running)
            {
                // MAKE SURE THIS LOCK ORDER IS THE SAME EVERYWHERE
                lock (_connections)
                {
                    lock (_messagesReceived)
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
                                int bytesRead = stream.Read(messageBuffer, 0, MessageBufferSize);
                                _messagesReceived[connection.Key].AddRange(messageBuffer.Take(bytesRead));
                                connection.Value.LastActiveTime = DateTime.Now;
                            }
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

        /// <summary>
        /// The run method of the message sender thread.
        /// </summary>
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
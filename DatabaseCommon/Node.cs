using Database.Common.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        /// The amount of seconds before a heartbeat is sent out to all connections.
        /// </summary>
        private const int HeartbeatInterval = 10;

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
        private readonly Dictionary<NodeDefinition, Connection> _connections = new Dictionary<NodeDefinition, Connection>();

        /// <summary>
        /// A collection of the current messages to be processed.
        /// </summary>
        private readonly Dictionary<NodeDefinition, List<byte>> _messagesReceived = new Dictionary<NodeDefinition, List<byte>>();

        /// <summary>
        /// A queue of the messages to be sent.
        /// </summary>
        private readonly Queue<Message> _messagesToSend = new Queue<Message>();

        /// <summary>
        /// The port the node is running on.
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// A collection of messages that are waiting for responses.
        /// </summary>
        private readonly Dictionary<uint, Tuple<Message, DateTime>> _waitingForResponses = new Dictionary<uint, Tuple<Message, DateTime>>();

        /// <summary>
        /// The thread dealing with various cleanup operations.
        /// </summary>
        private Thread _cleanerThread;

        /// <summary>
        /// The thread listening for new collections.
        /// </summary>
        private TcpListener _connectionListener;

        private Thread _heartbeatThread;

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
        protected Node(int port)
        {
            _port = port;
        }

        /// <summary>
        /// Gets a value indicating whether the node is running.
        /// </summary>
        public bool Running
        {
            get { return _running; }
        }

        protected Dictionary<NodeDefinition, Connection> Connections
        {
            get { return _connections; }
        }

        public ReadOnlyCollection<Tuple<NodeDefinition, NodeType>> GetConnectedNodes()
        {
            List<Tuple<NodeDefinition, NodeType>> list = new List<Tuple<NodeDefinition, NodeType>>();

            lock (_connections)
            {
                foreach (var item in _connections)
                {
                    list.Add(new Tuple<NodeDefinition, NodeType>(item.Key, item.Value.NodeType));
                }
            }

            return list.AsReadOnly();
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

            _heartbeatThread = new Thread(RunHeartbeat);
            _heartbeatThread.Start();
        }

        protected abstract void MessageReceived(Message message);

        protected void RenameConnection(NodeDefinition oldName, NodeDefinition newName)
        {
            // MAKE SURE THIS LOCK ORDER IS THE SAME EVERYWHERE
            lock (_connections)
            {
                lock (_messagesReceived)
                {
                    if (_connections.ContainsKey(oldName) && !_connections.ContainsKey(newName))
                    {
                        _connections.Add(newName, _connections[oldName]);
                        _connections.Remove(oldName);

                        if (_messagesReceived.ContainsKey(oldName))
                        {
                            _messagesReceived.Add(newName, _messagesReceived[oldName]);
                            _messagesReceived.Remove(oldName);
                        }
                    }
                }
            }
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

        private void MessageReceivedHandler(object message)
        {
            MessageReceived((Message)message);
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

            Connection connection = new Connection(incoming);
            lock (_connections)
            {
                NodeDefinition def = new NodeDefinition(((IPEndPoint)incoming.Client.RemoteEndPoint).Address.ToString(), ((IPEndPoint)incoming.Client.RemoteEndPoint).Port);
                _connections.Add(def, connection);
            }
        }

        /// <summary>
        /// Processes a message.
        /// </summary>
        /// <param name="address">The address the message was sent from.</param>
        /// <param name="data">The data to be decoded and processed.</param>
        private void ProcessMessage(NodeDefinition address, byte[] data)
        {
            Message message = new Message(address, data);

            // If the message is in response to another message, then don't send an event.
            // Also, if the message is a Heartbeat, we don't need to do anything,
            // the last connection time should have been updated when it was received.
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
            else if (!(message.Data is Heartbeat))
            {
                ThreadPool.QueueUserWorkItem(MessageReceivedHandler, message);
            }
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

                List<NodeDefinition> connectionsToRemove = new List<NodeDefinition>();
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

        private void RunHeartbeat()
        {
            for (int i = 0; i < HeartbeatInterval && _running; ++i)
            {
                Thread.Sleep(1);
            }

            while (_running)
            {
                lock (_connections)
                {
                    foreach (var connection in _connections)
                    {
                        ThreadPool.QueueUserWorkItem(SendHeartbeat, connection.Key);
                    }

                    for (int i = 0; i < HeartbeatInterval && _running; ++i)
                    {
                        Thread.Sleep(1);
                    }
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
                                connection.Value.ResetLastActiveTime();
                            }
                        }
                    }
                }

                List<Tuple<NodeDefinition, byte[]>> messages = new List<Tuple<NodeDefinition, byte[]>>();
                lock (_messagesReceived)
                {
                    foreach (var message in _messagesReceived)
                    {
                        if (message.Value.Count >= 4)
                        {
                            int length = BitConverter.ToInt32(message.Value.Take(4).ToArray(), 0);
                            if (message.Value.Count >= length + 4)
                            {
                                messages.Add(new Tuple<NodeDefinition, byte[]>(message.Key, message.Value.Skip(4).Take(length).ToArray()));
                                message.Value.RemoveRange(0, length + 4);
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
                            if ((_connections.ContainsKey(message.Address) &&
                                 _connections[message.Address].Status == ConnectionStatus.Connected) ||
                                message.SendWithoutConfirmation)
                            {
                                try
                                {
                                    if (!_connections.ContainsKey(message.Address))
                                    {
                                        TcpClient client = new TcpClient(message.Address.Hostname, message.Address.Port);
                                        _connections.Add(message.Address,
                                            new Connection(client));
                                    }

                                    if (message.WaitingForResponse)
                                    {
                                        lock (_waitingForResponses)
                                        {
                                            _waitingForResponses.Add(message.ID,
                                                new Tuple<Message, DateTime>(message, DateTime.UtcNow));
                                        }
                                    }

                                    byte[] dataToSend = message.EncodeMessage();
                                    var stream = _connections[message.Address].Client.GetStream();
                                    stream.Write(dataToSend, 0, dataToSend.Length);

                                    message.Status = message.WaitingForResponse
                                        ? MessageStatus.WaitingForResponse
                                        : MessageStatus.Sent;
                                }
                                catch (SocketException)
                                {
                                    message.Status = MessageStatus.SendingFailure;

                                    lock (_waitingForResponses)
                                    {
                                        _waitingForResponses.Remove(message.ID);
                                    }
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

        private void SendHeartbeat(object address)
        {
            SendMessage(new Message((NodeDefinition)address, new Heartbeat(), false));
        }
    }
}
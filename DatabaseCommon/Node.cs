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
        /// The amount of seconds between cleaner runs.
        /// </summary>
        private const int CleanerFrequency = 2;

        /// <summary>
        /// The amount of seconds before a connection causes a timeout.
        /// </summary>
        private const int ConnectionTimeout = 30;

        /// <summary>
        /// The amount of seconds before a heartbeat is sent out to all connections.
        /// </summary>
        private const int HeartbeatInterval = 5;

        /// <summary>
        /// The size of the buffer, in bytes, to be read at a time by the message listener.
        /// </summary>
        private const int MessageBufferSize = 1024;

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
        private readonly int? _port;

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

        /// <summary>
        /// The thread sending out heartbeats.
        /// </summary>
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
        /// The primary controller.
        /// </summary>
        private NodeDefinition _primary;

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
        /// Initializes a new instance of the <see cref="Node"/> class. Using this constructor disables the ability to receive connections.
        /// </summary>
        protected Node()
        {
            _port = null;
        }

        /// <summary>
        /// Gets or sets the current primary controller node.
        /// </summary>
        public NodeDefinition Primary
        {
            get
            {
                return _primary;
            }

            set
            {
                _primary = value;
                PrimaryChanged();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the node is running.
        /// </summary>
        public bool Running
        {
            get { return _running; }
        }

        /// <summary>
        /// Gets the node definition represents the current node.
        /// </summary>
        public abstract NodeDefinition Self { get; }

        /// <summary>
        /// Gets the current connections.
        /// </summary>
        protected Dictionary<NodeDefinition, Connection> Connections
        {
            get { return _connections; }
        }

        /// <summary>
        /// Gets a list of the connected nodes.
        /// </summary>
        /// <returns>A readonly list of the connected nodes.</returns>
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

                if (_connectionListener != null)
                {
                    _connectionListener.Stop();
                }
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

            if (_port.HasValue)
            {
                _connectionListener = new TcpListener(IPAddress.Any, _port.Value);
                _connectionListener.Start();
                _connectionListener.BeginAcceptTcpClient(ProcessConnectionRequest, null);
            }

            _cleanerThread = new Thread(RunCleaner);
            _cleanerThread.Start();

            _heartbeatThread = new Thread(RunHeartbeat);
            _heartbeatThread.Start();
        }

        /// <summary>
        /// Called when a connection is lost.
        /// </summary>
        /// <param name="node">The connection that was lost.</param>
        protected abstract void ConnectionLost(NodeDefinition node);

        /// <summary>
        /// Called when a message a received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        protected abstract void MessageReceived(Message message);

        /// <summary>
        /// Called when the primary controller has changed.
        /// </summary>
        protected abstract void PrimaryChanged();

        /// <summary>
        /// Renames a connection.
        /// </summary>
        /// <param name="currentName">The current name of the connection.</param>
        /// <param name="newName">The new name of the connection.</param>
        protected void RenameConnection(NodeDefinition currentName, NodeDefinition newName)
        {
            if (Equals(currentName, newName))
            {
                return;
            }

            // MAKE SURE THIS LOCK ORDER IS THE SAME EVERYWHERE
            lock (_connections)
            {
                lock (_messagesReceived)
                {
                    if (_connections.ContainsKey(currentName))
                    {
                        // Most likely means that a node disconnected and is trying to reconnect before the connection timeout time.
                        if (_connections.ContainsKey(newName))
                        {
                            _connections.Remove(newName);
                        }

                        if (_messagesReceived.ContainsKey(newName))
                        {
                            _messagesReceived.Remove(newName);
                        }

                        var connectionValue = _connections[currentName];
                        _connections.Remove(currentName);
                        _connections.Add(newName, connectionValue);

                        if (_messagesReceived.ContainsKey(currentName))
                        {
                            var messageValue = _messagesReceived[currentName];
                            _messagesReceived.Remove(currentName);
                            _messagesReceived.Add(newName, messageValue);
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

        /// <summary>
        /// Called when a connection is lost.
        /// </summary>
        /// <param name="node">The connection that was lost.</param>
        private void ConnectionLostHandler(object node)
        {
            ConnectionLost((NodeDefinition)node);
        }

        /// <summary>
        /// Called when a message is received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
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

            Logger.Log("Received message of type " + message.Data.GetType().Name + " from " + message.Address.ConnectionName, LogLevel.Debug);

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
                        if ((now - item.Value.Item2).TotalSeconds > item.Value.Item1.ResponseTimeout)
                        {
                            item.Value.Item1.Status = MessageStatus.ResponseTimeout;
                            responsesToRemove.Add(item.Key);
                        }
                    }

                    foreach (var item in responsesToRemove)
                    {
                        Logger.Log("Timeout while waiting for message response from " + _waitingForResponses[item].Item1.Address.ConnectionName, LogLevel.Info);
                        _waitingForResponses.Remove(item);
                    }
                }

                List<NodeDefinition> connectionsToRemove = new List<NodeDefinition>();
                lock (_connections)
                {
                    foreach (var connection in _connections)
                    {
                        if (!connection.Value.Client.Connected || (now - connection.Value.LastActiveTime).TotalSeconds > ConnectionTimeout || connection.Value.Status == ConnectionStatus.Disconnected)
                        {
                            connectionsToRemove.Add(connection.Key);
                        }
                    }

                    lock (_messagesReceived)
                    {
                        foreach (var connection in connectionsToRemove)
                        {
                            Logger.Log("Connection lost to " + connection.ConnectionName, LogLevel.Info);
                            _connections.Remove(connection);
                            _messagesReceived.Remove(connection);
                            ThreadPool.QueueUserWorkItem(ConnectionLostHandler, connection);
                        }
                    }
                }

                // Keep thread responsive to shutdown while waiting for next run (5 seconds).
                int i = 0;
                while (_running && i < CleanerFrequency)
                {
                    Thread.Sleep(1000);
                    ++i;
                }
            }
        }

        /// <summary>
        /// The run method of the heartbeat sending thread.
        /// </summary>
        private void RunHeartbeat()
        {
            for (int i = 0; i < HeartbeatInterval && _running; ++i)
            {
                Thread.Sleep(1000);
            }

            while (_running)
            {
                lock (_connections)
                {
                    foreach (var connection in _connections)
                    {
                        SendMessage(new Message(connection.Key, new Heartbeat(), false));
                    }
                }

                for (int i = 0; i < HeartbeatInterval && _running; ++i)
                {
                    Thread.Sleep(1000);
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

                        ThreadPool.QueueUserWorkItem(SendMessageWorkItem, message);
                    }
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Does the work of sending the messages.
        /// </summary>
        /// <param name="obj">The message to send.</param>
        private void SendMessageWorkItem(object obj)
        {
            Message message = (Message)obj;

            bool createConnection = false;
            lock (_connections)
            {
                if (!_connections.ContainsKey(message.Address) && message.SendWithoutConfirmation)
                {
                    createConnection = true;
                }
            }

            if (createConnection)
            {
                try
                {
                    TcpClient client = new TcpClient(message.Address.Hostname, message.Address.Port);
                    lock (_connections)
                    {
                        if (!_connections.ContainsKey(message.Address))
                        {
                            _connections.Add(message.Address, new Connection(client));
                        }
                    }
                }
                catch
                {
                    message.Status = MessageStatus.SendingFailure;
                    return;
                }
            }

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
                            message.Status = MessageStatus.SendingFailure;
                            return;
                        }

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

                        message.Status = message.WaitingForResponse
                            ? MessageStatus.WaitingForResponse
                            : MessageStatus.Sent;
                    }
                    catch
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
}
using Database.Common;
using Database.Common.DataOperation;
using Database.Common.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Database.Query
{
    /// <summary>
    /// Represents a query node.
    /// </summary>
    public class QueryNode : Node
    {
        /// <summary>
        /// The lock to use when accessing the chunk list.
        /// </summary>
        private readonly ReaderWriterLockSlim _chunkListLock = new ReaderWriterLockSlim();

        /// <summary>
        /// A list of the connected storage nodes.
        /// </summary>
        private readonly List<NodeDefinition> _connectedStorageNodes = new List<NodeDefinition>();

        /// <summary>
        /// The list of chunks known by the database.
        /// </summary>
        private List<ChunkDefinition> _chunkList = new List<ChunkDefinition>();

        /// <summary>
        /// A list of the controller nodes.
        /// </summary>
        private List<NodeDefinition> _controllerNodes;

        /// <summary>
        /// The thread that handles reconnecting to the controller nodes.
        /// </summary>
        private Thread _reconnectionThread;

        /// <summary>
        /// The settings of the query node.
        /// </summary>
        private QueryNodeSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryNode"/> class.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        public QueryNode(QueryNodeSettings settings)
            : base(settings.Port)
        {
            _settings = settings;
        }

        /// <inheritdoc />
        public override NodeDefinition Self
        {
            get { return new NodeDefinition(_settings.NodeName, _settings.Port); }
        }

        /// <inheritdoc />
        public override void Run()
        {
            BeforeStart();

            _controllerNodes = NodeDefinition.ParseConnectionString(_settings.ConnectionString);

            if (_controllerNodes.Any(def => !ConnectToController(def)))
            {
                return;
            }

            _reconnectionThread = new Thread(ReconnectionThreadRun);
            _reconnectionThread.Start();

            while (Running)
            {
                Thread.Sleep(100);
            }

            AfterStop();
        }

        /// <inheritdoc />
        protected override void ConnectionLost(NodeDefinition node, NodeType type)
        {
            if (Equals(Primary, node))
            {
                Logger.Log("Primary controller unreachable.", LogLevel.Info);
                Primary = null;
            }

            lock (_connectedStorageNodes)
            {
                if (_connectedStorageNodes.Contains(node))
                {
                    _connectedStorageNodes.Remove(node);
                }
            }
        }

        /// <inheritdoc />
        protected override void MessageReceived(Message message)
        {
            if (message.Data is JoinAttempt)
            {
                JoinAttempt attempt = (JoinAttempt)message.Data;
                if (attempt.Type != NodeType.Api)
                {
                    SendMessage(new Message(message, new JoinFailure("Only an API node can send a join attempt to a query node."), false)
                    {
                        SendWithoutConfirmation = true
                    });
                }

                if (attempt.Settings != _settings.ConnectionString)
                {
                    SendMessage(new Message(message, new JoinFailure("The connection strings do not match."), false)
                    {
                        SendWithoutConfirmation = true
                    });
                }

                Connections[message.Address].ConnectionEstablished(message.Address, attempt.Type);
                var response = new Message(message, new JoinSuccess(new Document()), true);
                SendMessage(response);
                response.BlockUntilDone();
            }
            else if (message.Data is PrimaryAnnouncement)
            {
                Logger.Log("Setting the primary controller to " + message.Address.ConnectionName, LogLevel.Info);
                Primary = message.Address;
            }
            else if (message.Data is NodeList)
            {
                lock (_connectedStorageNodes)
                {
                    _connectedStorageNodes.Clear();
                    _connectedStorageNodes.AddRange(((NodeList)message.Data).Nodes.Select(e => new NodeDefinition(e.Split(':')[0], int.Parse(e.Split(':')[1]))));

                    var connections = GetConnectedNodes();
                    foreach (var item in _connectedStorageNodes)
                    {
                        if (!connections.Any(e => Equals(e.Item1, item)))
                        {
                            Message attempt = new Message(item, new JoinAttempt(NodeType.Query, _settings.NodeName, _settings.Port, _settings.ConnectionString), true)
                            {
                                SendWithoutConfirmation = true
                            };

                            SendMessage(attempt);
                            attempt.BlockUntilDone();

                            if (attempt.Success)
                            {
                                if (attempt.Response.Data is JoinFailure)
                                {
                                    Logger.Log("Failed to join storage node: " + ((JoinFailure)attempt.Response.Data).Reason, LogLevel.Error);
                                }
                                else
                                {
                                    Connections[item].ConnectionEstablished(item, NodeType.Storage);
                                    SendMessage(new Message(attempt.Response, new Acknowledgement(), false));
                                }
                            }
                        }
                    }
                }
            }
            else if (message.Data is DataOperation)
            {
                DataOperation op = (DataOperation)message.Data;
                Document dataOperation = new Document(op.Json);
                if (!dataOperation.Valid)
                {
                    SendMessage(new Message(message, new DataOperationResult(ErrorCodes.InvalidDocument, "The document is invalid."), false));
                    return;
                }

                try
                {
                    ProcessDataOperation(dataOperation, message);
                }
                catch (Exception e)
                {
                    Logger.Log(e.Message + "\nStackTrace:" + e.StackTrace, LogLevel.Error);
                    SendMessage(new Message(message, new DataOperationResult(ErrorCodes.FailedMessage, "An exception occurred while processing the operation."), false));
                }
            }
            else if (message.Data is ChunkListUpdate)
            {
                _chunkListLock.EnterWriteLock();
                _chunkList = ((ChunkListUpdate)message.Data).ChunkList;
                _chunkListLock.ExitWriteLock();
                SendMessage(new Message(message, new Acknowledgement(), false));
            }
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
        }

        /// <summary>
        /// Connects to the specified controller.
        /// </summary>
        /// <param name="def">The controller to connect to.</param>
        /// <returns>True if the connection failed or return a JoinSuccess, false if it returned a JoinFailure.</returns>
        private bool ConnectToController(NodeDefinition def)
        {
            Message message = new Message(def, new JoinAttempt(NodeType.Query, _settings.NodeName, _settings.Port, _settings.ToString()), true)
            {
                SendWithoutConfirmation = true
            };

            SendMessage(message);
            message.BlockUntilDone();

            if (message.Success)
            {
                if (message.Response.Data is JoinFailure)
                {
                    Logger.Log("Failed to join controllers: " + ((JoinFailure)message.Response.Data).Reason, LogLevel.Error);
                    AfterStop();
                    return false;
                }

                // success
                JoinSuccess successData = (JoinSuccess)message.Response.Data;
                Connections[def].ConnectionEstablished(def, NodeType.Controller);
                if (successData.Data["PrimaryController"].ValueAsBoolean)
                {
                    Logger.Log("Setting the primary controller to " + message.Address.ConnectionName, LogLevel.Info);
                    Primary = message.Address;
                }

                SendMessage(new Message(message.Response, new Acknowledgement(), false));
            }

            return true;
        }

        /// <summary>
        /// Processes an add operation.
        /// </summary>
        /// <param name="dataOperation">The document that represents the operation.</param>
        /// <param name="message">The original message.</param>
        private void ProcessAddOperation(Document dataOperation, Message message)
        {
            AddOperation addOperation = new AddOperation(dataOperation["add"].ValueAsDocument);
            if (!addOperation.Valid)
            {
                SendMessage(new Message(message, addOperation.ErrorMessage, false));
                return;
            }

            _chunkListLock.EnterReadLock();
            var chunk = _chunkList.SingleOrDefault(e => ChunkMarker.IsBetween(e.Start, e.End, addOperation.Id.ToString()));
            _chunkListLock.ExitReadLock();

            NodeDefinition node = chunk == null ? null : chunk.Node;

            if (node == null)
            {
                SendMessage(new Message(message, new DataOperationResult(ErrorCodes.FailedMessage, "No storage node up for the specified id range."), false));
                return;
            }

            Message operationMessage = new Message(node, message.Data, true);
            operationMessage.SetResponseCallback(delegate(Message originalOperationMessage)
            {
                if (originalOperationMessage.Success)
                {
                    DataOperationResult result = (DataOperationResult)originalOperationMessage.Response.Data;
                    Document resultDocument = new Document(result.Result);
                    if (!resultDocument["success"].ValueAsBoolean && (ErrorCodes)Enum.Parse(typeof(ErrorCodes), resultDocument["errorcode"].ValueAsString) == ErrorCodes.ChunkMoved)
                    {
                        ProcessAddOperation(dataOperation, message);
                        return;
                    }

                    SendMessage(new Message(message, result, false));
                    return;
                }

                SendMessage(new Message(message, new DataOperationResult(ErrorCodes.FailedMessage, "Failed to send message to storage node."), false));
            });
            SendMessage(operationMessage);
        }

        /// <summary>
        /// Processes a data operation.
        /// </summary>
        /// <param name="dataOperation">The data operation to process.</param>
        /// <param name="message">The original message.</param>
        private void ProcessDataOperation(Document dataOperation, Message message)
        {
            if (dataOperation.ContainsKey("add") && dataOperation["add"].ValueType == DocumentEntryType.Document)
            {
                ProcessAddOperation(dataOperation, message);
                return;
            }

            if (dataOperation.ContainsKey("remove") && dataOperation["remove"].ValueType == DocumentEntryType.Document)
            {
                ProcessRemoveOperation(dataOperation, message);
                return;
            }

            if (dataOperation.ContainsKey("update") && dataOperation["update"].ValueType == DocumentEntryType.Document)
            {
                ProcessUpdateOperation(dataOperation, message);
                return;
            }

            if (dataOperation.ContainsKey("query") && dataOperation["query"].ValueType == DocumentEntryType.Document)
            {
                ProcessQueryOperation(dataOperation, message);
                return;
            }

            SendMessage(new Message(message, new DataOperationResult(ErrorCodes.InvalidDocument, "No valid operation specified."), false));
        }

        /// <summary>
        /// Processes a query operation.
        /// </summary>
        /// <param name="dataOperation">The document that represents the operation.</param>
        /// <param name="message">The original message.</param>
        private void ProcessQueryOperation(Document dataOperation, Message message)
        {
            List<Message> sent = new List<Message>();
            lock (_connectedStorageNodes)
            {
                foreach (var item in _connectedStorageNodes)
                {
                    Message operationMessage = new Message(item, message.Data, true);
                    SendMessage(operationMessage);
                    sent.Add(operationMessage);
                }
            }

            Document workingDocument = new Document();
            int i = 0;
            foreach (var result in sent)
            {
                result.BlockUntilDone();
                if (result.Success)
                {
                    Document doc = new Document(((DataOperationResult)result.Response.Data).Result);
                    if (doc["success"].ValueAsBoolean)
                    {
                        Document results = doc["result"].ValueAsDocument;
                        for (int j = 0; j < results["count"].ValueAsInteger; ++j)
                        {
                            workingDocument[i.ToString()] = new DocumentEntry(i.ToString(), results[j.ToString()].ValueType, results[j.ToString()].Value);
                            ++i;
                        }
                    }
                    else
                    {
                        if ((ErrorCodes)Enum.Parse(typeof(ErrorCodes), doc["errorcode"].ValueAsString) != ErrorCodes.ChunkMoved)
                        {
                            SendMessage(new Message(message, new DataOperationResult((DataOperationResult)result.Response.Data), false));
                            return;
                        }
                    }
                }
                else
                {
                    SendMessage(new Message(message, new DataOperationResult(ErrorCodes.FailedMessage, "Could not reach any storage nodes."), false));
                    return;
                }
            }

            if (sent.Count == 0)
            {
                SendMessage(new Message(message, new DataOperationResult(ErrorCodes.FailedMessage, "Could not reach any storage nodes."), false));
                return;
            }

            workingDocument["count"] = new DocumentEntry("count", DocumentEntryType.Integer, i);
            SendMessage(new Message(message, new DataOperationResult(workingDocument), false));
        }

        /// <summary>
        /// Processes a remove operation.
        /// </summary>
        /// <param name="dataOperation">The document that represents the operation.</param>
        /// <param name="message">The original message.</param>
        private void ProcessRemoveOperation(Document dataOperation, Message message)
        {
            RemoveOperation removeOperation = new RemoveOperation(dataOperation["remove"].ValueAsDocument);
            if (!removeOperation.Valid)
            {
                SendMessage(new Message(message, removeOperation.ErrorMessage, false));
                return;
            }

            _chunkListLock.EnterReadLock();
            var chunk = _chunkList.SingleOrDefault(e => ChunkMarker.IsBetween(e.Start, e.End, removeOperation.DocumentId.ToString()));
            _chunkListLock.ExitReadLock();

            NodeDefinition node = chunk == null ? null : chunk.Node;

            if (node == null)
            {
                SendMessage(new Message(message, new DataOperationResult(ErrorCodes.FailedMessage, "No storage node up for the specified id range."), false));
                return;
            }

            Message operationMessage = new Message(node, message.Data, true);
            operationMessage.SetResponseCallback(delegate(Message originalOperationMessage)
            {
                if (originalOperationMessage.Success)
                {
                    DataOperationResult result = (DataOperationResult)originalOperationMessage.Response.Data;
                    Document resultDocument = new Document(result.Result);
                    if (!resultDocument["success"].ValueAsBoolean && (ErrorCodes)Enum.Parse(typeof(ErrorCodes), resultDocument["errorcode"].ValueAsString) == ErrorCodes.ChunkMoved)
                    {
                        ProcessRemoveOperation(dataOperation, message);
                        return;
                    }

                    SendMessage(new Message(message, result, false));
                    return;
                }

                SendMessage(new Message(message, new DataOperationResult(ErrorCodes.FailedMessage, "Failed to send message to storage node."), false));
            });
            SendMessage(operationMessage);
        }

        /// <summary>
        /// Processes an update operation.
        /// </summary>
        /// <param name="dataOperation">The document that represents the operation.</param>
        /// <param name="message">The original message.</param>
        private void ProcessUpdateOperation(Document dataOperation, Message message)
        {
            UpdateOperation updateOperation = new UpdateOperation(dataOperation["update"].ValueAsDocument);
            if (!updateOperation.Valid)
            {
                SendMessage(new Message(message, updateOperation.ErrorMessage, false));
                return;
            }

            _chunkListLock.EnterReadLock();
            var chunk = _chunkList.SingleOrDefault(e => ChunkMarker.IsBetween(e.Start, e.End, updateOperation.DocumentId.ToString()));
            _chunkListLock.ExitReadLock();

            NodeDefinition node = chunk == null ? null : chunk.Node;

            if (node == null)
            {
                SendMessage(new Message(message, new DataOperationResult(ErrorCodes.FailedMessage, "No storage node up for the specified id range."), false));
                return;
            }

            Message operationMessage = new Message(node, message.Data, true);
            operationMessage.SetResponseCallback(delegate(Message originalOperationMessage)
            {
                if (originalOperationMessage.Success)
                {
                    DataOperationResult result = (DataOperationResult)originalOperationMessage.Response.Data;
                    Document resultDocument = new Document(result.Result);
                    if (!resultDocument["success"].ValueAsBoolean && (ErrorCodes)Enum.Parse(typeof(ErrorCodes), resultDocument["errorcode"].ValueAsString) == ErrorCodes.ChunkMoved)
                    {
                        ProcessUpdateOperation(dataOperation, message);
                        return;
                    }

                    SendMessage(new Message(message, result, false));
                    return;
                }

                SendMessage(new Message(message, new DataOperationResult(ErrorCodes.FailedMessage, "Failed to send message to storage node."), false));
            });
            SendMessage(operationMessage);
        }

        /// <summary>
        /// Runs the thread that handles reconnecting to the controllers.
        /// </summary>
        private void ReconnectionThreadRun()
        {
            Random rand = new Random();

            int timeToWait = rand.Next(30, 60);
            int i = 0;
            while (i < timeToWait && Running)
            {
                Thread.Sleep(1000);
                ++i;
            }

            while (Running)
            {
                var connections = GetConnectedNodes();
                foreach (var def in _controllerNodes)
                {
                    if (!connections.Any(e => Equals(e.Item1, def)))
                    {
                        Logger.Log("Attempting to reconnect to " + def.ConnectionName, LogLevel.Info);
                        ConnectToController(def);
                    }
                }

                timeToWait = rand.Next(30, 60);
                i = 0;
                while (i < timeToWait && Running)
                {
                    Thread.Sleep(1000);
                    ++i;
                }
            }
        }
    }
}
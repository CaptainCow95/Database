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
        /// The list of chunks known by the database.
        /// </summary>
        private List<Tuple<ChunkMarker, ChunkMarker, NodeDefinition>> _chunkList = new List<Tuple<ChunkMarker, ChunkMarker, NodeDefinition>>();

        /// <summary>
        /// The lock to use when accessing the chunk list.
        /// </summary>
        private ReaderWriterLockSlim _chunkListLock = new ReaderWriterLockSlim();

        /// <summary>
        /// A list of the connected storage nodes.
        /// </summary>
        private List<NodeDefinition> _connectedStorageNodes = new List<NodeDefinition>();

        /// <summary>
        /// A list of the controller nodes.
        /// </summary>
        private List<NodeDefinition> _controllerNodes;

        /// <summary>
        /// The settings of the query node.
        /// </summary>
        private QueryNodeSettings _settings;

        /// <summary>
        /// The thread that attempts to reconnect to the necessary nodes.
        /// </summary>
        private Thread _updateThread;

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

            foreach (var def in _controllerNodes)
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
                        return;
                    }

                    // success
                    JoinSuccess successData = (JoinSuccess)message.Response.Data;
                    Connections[def].ConnectionEstablished(def, NodeType.Controller);
                    if (successData.PrimaryController)
                    {
                        Logger.Log("Setting the primary controller to " + message.Address.ConnectionName, LogLevel.Info);
                        Primary = message.Address;
                    }
                }
            }

            _updateThread = new Thread(UpdateThreadRun);
            _updateThread.Start();

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
                    SendMessage(new Message(message, new JoinFailure("Only an API node can send a join attempt to a query node."), false));
                }

                if (attempt.Settings != _settings.ConnectionString)
                {
                    SendMessage(new Message(message, new JoinFailure("The connection strings do not match."), false));
                }

                Connections[message.Address].ConnectionEstablished(message.Address, attempt.Type);
                SendMessage(new Message(message, new JoinSuccess(false), false));
            }
            else if (message.Data is NodeList)
            {
                lock (_connectedStorageNodes)
                {
                    _connectedStorageNodes = ((NodeList)message.Data).Nodes.Select(e => new NodeDefinition(e.Split(':')[0], int.Parse(e.Split(':')[1]))).ToList();

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

                DataOperationResult operationResult = ProcessDataOperation(dataOperation, op);

                SendMessage(new Message(message, operationResult, false));
            }
            else if (message.Data is ChunkListUpdate)
            {
                _chunkListLock.EnterWriteLock();
                _chunkList = ((ChunkListUpdate)message.Data).ChunkList;
                _chunkListLock.ExitWriteLock();
            }
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
        }

        /// <summary>
        /// Processes an add operation.
        /// </summary>
        /// <param name="dataOperation">The document that represents the operation.</param>
        /// <param name="op">The original operation message.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessAddOperation(Document dataOperation, DataOperation op)
        {
            AddOperation addOperation = new AddOperation(dataOperation["add"].ValueAsDocument);
            if (!addOperation.Valid)
            {
                return addOperation.ErrorMessage;
            }

            _chunkListLock.EnterReadLock();
            var chunk = _chunkList.SingleOrDefault(e => ChunkMarker.IsBetween(e.Item1, e.Item2, addOperation.Id.ToString()));
            _chunkListLock.ExitReadLock();

            NodeDefinition node = chunk == null ? null : chunk.Item3;

            if (node == null)
            {
                return new DataOperationResult(ErrorCodes.FailedMessage, "No storage node up for the specified id range.");
            }

            Message operationMessage = new Message(node, op, true);
            SendMessage(operationMessage);
            operationMessage.BlockUntilDone();
            if (operationMessage.Success)
            {
                return (DataOperationResult)operationMessage.Response.Data;
            }

            return new DataOperationResult(ErrorCodes.FailedMessage, "Failed to send message to storage node.");
        }

        /// <summary>
        /// Processes a data operation.
        /// </summary>
        /// <param name="dataOperation">The data operation to process.</param>
        /// <param name="op">The original data operation to pass on to the storage nodes.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessDataOperation(Document dataOperation, DataOperation op)
        {
            if (dataOperation.ContainsKey("add") && dataOperation["add"].ValueType == DocumentEntryType.Document)
            {
                return ProcessAddOperation(dataOperation, op);
            }

            if (dataOperation.ContainsKey("remove") && dataOperation["remove"].ValueType == DocumentEntryType.Document)
            {
                return ProcessRemoveOperation(dataOperation, op);
            }

            if (dataOperation.ContainsKey("update") && dataOperation["update"].ValueType == DocumentEntryType.Document)
            {
                return ProcessUpdateOperation(dataOperation, op);
            }

            if (dataOperation.ContainsKey("query") && dataOperation["query"].ValueType == DocumentEntryType.Document)
            {
                return ProcessQueryOperation(op);
            }

            return new DataOperationResult(ErrorCodes.InvalidDocument, "No valid operation specified.");
        }

        /// <summary>
        /// Processes a query operation.
        /// </summary>
        /// <param name="op">The original operation message.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessQueryOperation(DataOperation op)
        {
            List<Message> sent = new List<Message>();
            lock (_connectedStorageNodes)
            {
                foreach (var item in _connectedStorageNodes)
                {
                    Message operationMessage = new Message(item, op, true);
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
                        return new DataOperationResult((DataOperationResult)result.Response.Data);
                    }
                }
                else
                {
                    return new DataOperationResult(ErrorCodes.FailedMessage, "Could not reach any storage nodes.");
                }
            }

            if (sent.Count == 0)
            {
                return new DataOperationResult(ErrorCodes.FailedMessage, "Could not reach any storage nodes.");
            }

            workingDocument["count"] = new DocumentEntry("count", DocumentEntryType.Integer, i);
            return new DataOperationResult(workingDocument);
        }

        /// <summary>
        /// Processes a remove operation.
        /// </summary>
        /// <param name="dataOperation">The document that represents the operation.</param>
        /// <param name="op">The original operation message.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessRemoveOperation(Document dataOperation, DataOperation op)
        {
            RemoveOperation removeOperation = new RemoveOperation(dataOperation["remove"].ValueAsDocument);
            if (!removeOperation.Valid)
            {
                return removeOperation.ErrorMessage;
            }

            _chunkListLock.EnterReadLock();
            var chunk = _chunkList.SingleOrDefault(e => ChunkMarker.IsBetween(e.Item1, e.Item2, removeOperation.DocumentId.ToString()));
            _chunkListLock.ExitReadLock();

            NodeDefinition node = chunk == null ? null : chunk.Item3;

            if (node == null)
            {
                return new DataOperationResult(ErrorCodes.FailedMessage, "No storage node up for the specified id range.");
            }

            Message operationMessage = new Message(node, op, true);
            SendMessage(operationMessage);
            operationMessage.BlockUntilDone();
            if (operationMessage.Success)
            {
                return (DataOperationResult)operationMessage.Response.Data;
            }

            return new DataOperationResult(ErrorCodes.FailedMessage, "Failed to send message to storage node.");
        }

        /// <summary>
        /// Processes an update operation.
        /// </summary>
        /// <param name="dataOperation">The document that represents the operation.</param>
        /// <param name="op">The original operation message.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessUpdateOperation(Document dataOperation, DataOperation op)
        {
            UpdateOperation updateOperation = new UpdateOperation(dataOperation["update"].ValueAsDocument);
            if (!updateOperation.Valid)
            {
                return updateOperation.ErrorMessage;
            }

            _chunkListLock.EnterReadLock();
            var chunk = _chunkList.SingleOrDefault(e => ChunkMarker.IsBetween(e.Item1, e.Item2, updateOperation.DocumentId.ToString()));
            _chunkListLock.ExitReadLock();

            NodeDefinition node = chunk == null ? null : chunk.Item3;

            if (node == null)
            {
                return new DataOperationResult(ErrorCodes.FailedMessage, "No storage node up for the specified id range.");
            }

            Message operationMessage = new Message(node, op, true);
            SendMessage(operationMessage);
            operationMessage.BlockUntilDone();
            if (operationMessage.Success)
            {
                return (DataOperationResult)operationMessage.Response.Data;
            }

            return new DataOperationResult(ErrorCodes.FailedMessage, "Failed to send message to storage node.");
        }

        /// <summary>
        /// Attempts to reconnect to the necessary nodes.
        /// </summary>
        private void UpdateThreadRun()
        {
            Random rand = new Random();

            int timeToWait = rand.Next(30, 120);
            int i = 0;
            while (i < timeToWait && Running)
            {
                Thread.Sleep(1000);
                ++i;
            }

            while (Running)
            {
                timeToWait = rand.Next(30, 120);
                i = 0;
                while (i < timeToWait && Running)
                {
                    Thread.Sleep(1000);
                    ++i;
                }

                var connections = GetConnectedNodes();
                foreach (var def in _controllerNodes.Concat(_connectedStorageNodes))
                {
                    if (!connections.Any(e => Equals(e.Item1, def)))
                    {
                        Logger.Log("Attempting to reconnect to " + def.ConnectionName, LogLevel.Info);

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
                                return;
                            }

                            // success
                            JoinSuccess successData = (JoinSuccess)message.Response.Data;
                            Connections[def].ConnectionEstablished(def, NodeType.Controller);
                            if (successData.PrimaryController)
                            {
                                Logger.Log("Setting the primary controller to " + message.Address.ConnectionName, LogLevel.Info);
                                Primary = message.Address;
                            }
                        }
                    }
                }
            }
        }
    }
}
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
        /// A list of the connected storage nodes.
        /// </summary>
        private List<NodeDefinition> _connectedStorageNodes = new List<NodeDefinition>();

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

            List<NodeDefinition> controllerNodes = NodeDefinition.ParseConnectionString(_settings.ConnectionString);

            foreach (var def in controllerNodes)
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
                    Connections[def].ConnectionEstablished(NodeType.Controller);
                    if (successData.PrimaryController)
                    {
                        Logger.Log("Setting the primary controller to " + message.Address.ConnectionName, LogLevel.Info);
                        Primary = message.Address;
                    }
                }
            }

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

                Connections[message.Address].ConnectionEstablished(attempt.Type);
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
                                    Connections[item].ConnectionEstablished(NodeType.Storage);
                                }
                            }
                        }
                    }

                    // TODO: Actually connect to any new storage nodes.
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
                lock (_chunkList)
                {
                    _chunkList = ((ChunkListUpdate)message.Data).ChunkList;
                }
            }
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
        }

        /// <summary>
        /// Processes a data operation.
        /// </summary>
        /// <param name="dataOperation">The data operation to process.</param>
        /// <param name="op">The original data operation to pass on to the storage nodes.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessDataOperation(Document dataOperation, DataOperation op)
        {
            if (dataOperation.ContainsKey("add") && dataOperation["add"].ValueType == DocumentEntryType.Document &&
                dataOperation["add"].ValueAsDocument.ContainsKey("document") && dataOperation["add"].ValueAsDocument["document"].ValueType == DocumentEntryType.Document)
            {
                if (dataOperation["add"].ValueAsDocument["document"].ValueAsDocument.ContainsKey("id"))
                {
                    if (dataOperation["add"].ValueAsDocument["document"].ValueAsDocument["id"].ValueType == DocumentEntryType.String)
                    {
                        ObjectId id;
                        try
                        {
                            id = new ObjectId(dataOperation["add"].ValueAsDocument["document"].ValueAsDocument["id"].ValueAsString);
                        }
                        catch (Exception)
                        {
                            return new DataOperationResult(ErrorCodes.InvalidId, "Document contains an id field that is not an ObjectId.");
                        }

                        NodeDefinition node = null;
                        lock (_chunkList)
                        {
                            foreach (var item in _chunkList)
                            {
                                if (ChunkMarker.IsBetween(item.Item1, item.Item2, id.ToString()))
                                {
                                    node = item.Item3;
                                    break;
                                }
                            }
                        }

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

                    return new DataOperationResult(ErrorCodes.InvalidId, "Document contains an id field that is not an ObjectId.");
                }

                return new DataOperationResult(ErrorCodes.InvalidId, "Document does not contain an id field.");
            }

            if (dataOperation.ContainsKey("remove") && dataOperation["remove"].ValueType == DocumentEntryType.Document)
            {
                if (dataOperation["remove"].ValueAsDocument.ContainsKey("documentId"))
                {
                    if (dataOperation["remove"].ValueAsDocument["documentId"].ValueType == DocumentEntryType.String)
                    {
                        ObjectId id;
                        try
                        {
                            id = new ObjectId(dataOperation["remove"].ValueAsDocument["documentId"].ValueAsString);
                        }
                        catch (Exception)
                        {
                            return new DataOperationResult(ErrorCodes.InvalidDocument, "The document is invalid.");
                        }

                        NodeDefinition node = null;
                        lock (_chunkList)
                        {
                            foreach (var item in _chunkList)
                            {
                                if (ChunkMarker.IsBetween(item.Item1, item.Item2, id.ToString()))
                                {
                                    node = item.Item3;
                                    break;
                                }
                            }
                        }

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
                }

                return new DataOperationResult(ErrorCodes.InvalidDocument, "The document is invalid.");
            }

            if (dataOperation.ContainsKey("update") && dataOperation["update"].ValueType == DocumentEntryType.Document)
            {
                if (dataOperation["update"].ValueAsDocument.ContainsKey("documentId"))
                {
                    if (dataOperation["update"].ValueAsDocument["documentId"].ValueType == DocumentEntryType.String)
                    {
                        ObjectId id;
                        try
                        {
                            id = new ObjectId(dataOperation["update"].ValueAsDocument["documentId"].ValueAsString);
                        }
                        catch (Exception)
                        {
                            return new DataOperationResult(ErrorCodes.InvalidDocument, "The document is invalid.");
                        }

                        NodeDefinition node = null;
                        lock (_chunkList)
                        {
                            foreach (var item in _chunkList)
                            {
                                if (ChunkMarker.IsBetween(item.Item1, item.Item2, id.ToString()))
                                {
                                    node = item.Item3;
                                    break;
                                }
                            }
                        }

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
                }

                return new DataOperationResult(ErrorCodes.InvalidDocument, "The document is invalid.");
            }

            if (dataOperation.ContainsKey("query") && dataOperation["query"].ValueType == DocumentEntryType.Document)
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

            return new DataOperationResult(ErrorCodes.InvalidDocument, "No valid operation specified.");
        }
    }
}
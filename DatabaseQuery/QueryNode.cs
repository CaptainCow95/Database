using Database.Common;
using Database.Common.DataOperation;
using Database.Common.Messages;
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
        protected override void ConnectionLost(NodeDefinition node)
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

                Document operationResult = new Document();
                if (dataOperation.ContainsKey("query"))
                {
                    bool success = true;
                    int i = 0;
                    foreach (var result in sent)
                    {
                        result.BlockUntilDone();
                        if (result.Success)
                        {
                            Document doc = new Document(((DataOperationResult)result.Response.Data).Result);
                            if (doc["success"].ValueAsBoolean.Value)
                            {
                                Document results = doc["results"].ValueAsDocument;
                                for (int j = 0; j < results["count"].ValueAsInteger; ++j)
                                {
                                    operationResult[i.ToString()] = new DocumentEntry(i.ToString(), results[j.ToString()].ValueType, results[j.ToString()].Value);
                                    ++i;
                                }
                            }
                            else
                            {
                                operationResult = doc;
                                success = false;
                                break;
                            }
                        }
                    }

                    if (success)
                    {
                        operationResult["count"] = new DocumentEntry("count", DocumentEntryType.Integer, i);
                        operationResult = new Document("{\"success\":true,\"results\":" + operationResult.ToJson() + "}");
                    }
                }
                else
                {
                    if (sent.Count > 0)
                    {
                        sent[0].BlockUntilDone();
                        if (sent[0].Success)
                        {
                            operationResult = new Document(((DataOperationResult)sent[0].Response.Data).Result);
                        }
                    }
                }

                if (operationResult.Count == 0)
                {
                    operationResult = new Document("{\"success\":false,\"error\":\"Could not reach any storage nodes.\"}");
                }

                SendMessage(new Message(message, new DataOperationResult(operationResult.ToJson()), false));
            }
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
        }
    }
}
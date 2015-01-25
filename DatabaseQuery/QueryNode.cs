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
                Thread.Sleep(1);
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
            if (message.Data is StorageNodeConnection)
            {
                lock (_connectedStorageNodes)
                {
                    _connectedStorageNodes = ((StorageNodeConnection)message.Data).StorageNodes.Select(e => new NodeDefinition(e.Split(':')[0], int.Parse(e.Split(':')[1]))).ToList();

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

                Document operationResult = new Document("{}");
                foreach (var result in sent)
                {
                    result.BlockUntilDone();
                    if (result.Success)
                    {
                        Document doc = new Document(((DataOperationResult)result.Response.Data).Result);
                        if (doc["success"].ValueAsBoolean.Value)
                        {
                            operationResult.Merge(doc);
                        }
                    }
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
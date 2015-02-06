using Database.Common.DataOperation;
using Database.Common.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Database.Common.API
{
    /// <summary>
    /// The API entrance for the database.
    /// </summary>
    public class Database
    {
        /// <summary>
        /// The internal node.
        /// </summary>
        private readonly ApiNode _node;

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public Database(string connectionString)
        {
            _node = new ApiNode(connectionString);

            _node.Run();
            _node.WaitUntilFirstNodeListMessage();
        }

        /// <summary>
        /// Adds a document.
        /// </summary>
        /// <param name="doc">The document to add.</param>
        /// <returns>The resulting document.</returns>
        public Document Add(Document doc)
        {
            if (doc.ContainsKey("id"))
            {
                return _node.SendMessageToQuery(new Document("{\"add\":{\"document\":" + doc.ToJson() + "}}"));
            }

            while (true)
            {
                doc["id"] = new DocumentEntry("id", DocumentEntryType.String, new ObjectId().ToString());
                Document result = _node.SendMessageToQuery(new Document("{\"add\":{\"document\":" + doc.ToJson() + "}}"));
                if (!result["success"].ValueAsBoolean && (ErrorCodes)Enum.Parse(typeof(ErrorCodes), result["errorcode"].ValueAsString) == ErrorCodes.InvalidId)
                {
                    doc["id"] = new DocumentEntry("id", DocumentEntryType.String, new ObjectId().ToString());
                }
                else
                {
                    return result;
                }
            }
        }

        /// <summary>
        /// Queries for documents.
        /// </summary>
        /// <param name="doc">The query document.</param>
        /// <returns>The resulting document.</returns>
        public Document Query(Document doc)
        {
            return _node.SendMessageToQuery(new Document("{\"query\":{\"fields\":" + doc.ToJson() + "}}"));
        }

        /// <summary>
        /// Removes a document.
        /// </summary>
        /// <param name="documentId">The document id to remove.</param>
        /// <returns>The resulting document.</returns>
        public Document Remove(string documentId)
        {
            return _node.SendMessageToQuery(new Document("{\"remove\":{\"documentId\":\"" + documentId + "\"}}"));
        }

        /// <summary>
        /// Shutdown the internal node.
        /// </summary>
        public void Shutdown()
        {
            _node.Shutdown();
        }

        /// <summary>
        /// Updates a document.
        /// </summary>
        /// <param name="doc">The update document.</param>
        /// <returns>The resulting document.</returns>
        public Document Update(Document doc)
        {
            return _node.SendMessageToQuery(new Document("{\"update\":" + doc.ToJson() + "}"));
        }

        /// <summary>
        /// Represents a node used by the API.
        /// </summary>
        private class ApiNode : Node
        {
            /// <summary>
            /// The connection string.
            /// </summary>
            private readonly string _connectionString;

            /// <summary>
            /// The random number generator.
            /// </summary>
            private readonly Random _rand = new Random();

            /// <summary>
            /// A list of the connected query nodes.
            /// </summary>
            private List<NodeDefinition> _connectedQueryNodes = new List<NodeDefinition>();

            /// <summary>
            /// A value indicating whether the first node list message has been received.
            /// </summary>
            private bool _receivedFirstNodeListMessage = false;

            /// <summary>
            /// Initializes a new instance of the <see cref="ApiNode"/> class.
            /// </summary>
            /// <param name="connectionString">The connection string.</param>
            public ApiNode(string connectionString)
                : base()
            {
                _connectionString = connectionString;
            }

            /// <inheritdoc />
            public override NodeDefinition Self
            {
                get { throw new NotImplementedException(); }
            }

            /// <inheritdoc />
            public override void Run()
            {
                BeforeStart();

                List<NodeDefinition> controllerNodes = NodeDefinition.ParseConnectionString(_connectionString);

                foreach (var def in controllerNodes)
                {
                    Message message = new Message(def, new JoinAttempt(NodeType.Api, string.Empty, -1, _connectionString), true)
                    {
                        SendWithoutConfirmation = true
                    };

                    SendMessage(message);
                    message.BlockUntilDone();

                    if (message.Success)
                    {
                        if (message.Response.Data is JoinFailure)
                        {
                            AfterStop();
                            return;
                        }

                        JoinSuccess successData = (JoinSuccess)message.Response.Data;
                        Connections[def].ConnectionEstablished(message.Address, NodeType.Controller);
                        if (successData.PrimaryController)
                        {
                            Primary = message.Address;
                        }
                    }
                }
            }

            /// <summary>
            /// Sends a message to a query node.
            /// </summary>
            /// <param name="doc">The document to send.</param>
            /// <returns>The document that was returned.</returns>
            public Document SendMessageToQuery(Document doc)
            {
                Message message;
                lock (_connectedQueryNodes)
                {
                    if (_connectedQueryNodes.Count == 0)
                    {
                        return new Document("{\"success\":false,\"error\":\"No query nodes connected.\"}");
                    }

                    message = new Message(_connectedQueryNodes[_rand.Next(_connectedQueryNodes.Count)], new Messages.DataOperation(doc.ToJson()), true);
                }

                SendMessage(message);
                message.BlockUntilDone();

                if (message.Success)
                {
                    return new Document(((DataOperationResult)message.Response.Data).Result);
                }

                return new Document("{\"success\":false,\"error\":\"Failed to connect to a query node.\"}");
            }

            /// <summary>
            /// Shutdown the node.
            /// </summary>
            public void Shutdown()
            {
                AfterStop();
            }

            /// <summary>
            /// Blocks until the first NodeList message is received.
            /// </summary>
            internal void WaitUntilFirstNodeListMessage()
            {
                while (!_receivedFirstNodeListMessage)
                {
                    Thread.Sleep(1);
                }
            }

            /// <inheritdoc />
            protected override void ConnectionLost(NodeDefinition node, NodeType type)
            {
            }

            /// <inheritdoc />
            protected override void MessageReceived(Message message)
            {
                if (message.Data is NodeList)
                {
                    lock (_connectedQueryNodes)
                    {
                        _connectedQueryNodes = ((NodeList)message.Data).Nodes.Select(e => new NodeDefinition(e.Split(':')[0], int.Parse(e.Split(':')[1]))).ToList();

                        var connections = GetConnectedNodes();
                        foreach (var item in _connectedQueryNodes)
                        {
                            if (!connections.Any(e => Equals(e.Item1, item)))
                            {
                                Message attempt = new Message(item, new JoinAttempt(NodeType.Api, string.Empty, -1, _connectionString), true)
                                {
                                    SendWithoutConfirmation = true
                                };

                                SendMessage(attempt);
                                attempt.BlockUntilDone();

                                if (attempt.Success)
                                {
                                    if (attempt.Response.Data is JoinFailure)
                                    {
                                        // Log it?
                                    }
                                    else
                                    {
                                        Connections[item].ConnectionEstablished(attempt.Address, NodeType.Query);
                                    }
                                }
                            }
                        }
                    }

                    _receivedFirstNodeListMessage = true;
                }
            }

            /// <inheritdoc />
            protected override void PrimaryChanged()
            {
            }
        }
    }
}
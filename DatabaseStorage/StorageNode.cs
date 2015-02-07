using Database.Common;
using Database.Common.DataOperation;
using Database.Common.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Database.Storage
{
    /// <summary>
    /// Represents a storage node.
    /// </summary>
    public class StorageNode : Node
    {
        /// <summary>
        /// A list of the controller nodes.
        /// </summary>
        private List<NodeDefinition> _controllerNodes;

        /// <summary>
        /// The database of the node.
        /// </summary>
        private Database _database;

        /// <summary>
        /// The settings of the storage node.
        /// </summary>
        private StorageNodeSettings _settings;

        /// <summary>
        /// The thread that attempts to reconnect to the necessary nodes.
        /// </summary>
        private Thread _updateThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageNode"/> class.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        public StorageNode(StorageNodeSettings settings)
            : base(settings.Port)
        {
            _settings = settings;
            _database = new Database(this);
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
                Message message = new Message(def, new JoinAttempt(NodeType.Storage, _settings.NodeName, _settings.Port, _settings.ToString()), true)
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
                        _database.Shutdown();
                        return;
                    }

                    // success
                    JoinSuccess successData = (JoinSuccess)message.Response.Data;
                    Connections[def].ConnectionEstablished(def, NodeType.Controller);
                    if (successData.Data["PrimaryController"].ValueAsBoolean)
                    {
                        Logger.Log("Setting the primary controller to " + message.Address.ConnectionName, LogLevel.Info);
                        Primary = message.Address;

                        _database.SetMaxChunkItemCount(successData.Data["MaxChunkItemCount"].ValueAsInteger);
                    }

                    SendMessage(new Message(message.Response, new Acknowledgement(), false));
                }
            }

            _updateThread = new Thread(UpdateThreadRun);
            _updateThread.Start();

            while (Running)
            {
                Thread.Sleep(100);
            }

            AfterStop();
            _database.Shutdown();
        }

        /// <summary>
        /// Allows the database to send a message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        internal void SendDatabaseMessage(Message message)
        {
            SendMessage(message);
        }

        /// <inheritdoc />
        protected override void ConnectionLost(NodeDefinition node, NodeType type)
        {
            if (Equals(Primary, node))
            {
                Logger.Log("Primary controller unreachable.", LogLevel.Info);
                Primary = null;
            }
        }

        /// <inheritdoc />
        protected override void MessageReceived(Message message)
        {
            if (message.Data is JoinAttempt)
            {
                JoinAttempt attempt = (JoinAttempt)message.Data;
                if (attempt.Type != NodeType.Query)
                {
                    SendMessage(new Message(message, new JoinFailure("Only a query node can send a join attempt to a storage node."), false));
                }

                if (attempt.Settings != _settings.ConnectionString)
                {
                    SendMessage(new Message(message, new JoinFailure("The connection strings do not match."), false));
                }

                NodeDefinition nodeDef = new NodeDefinition(attempt.Name, attempt.Port);
                RenameConnection(message.Address, nodeDef);
                Connections[nodeDef].ConnectionEstablished(nodeDef, attempt.Type);
                Message response = new Message(message, new JoinSuccess(new Document()), true)
                {
                    Address = nodeDef
                };

                SendMessage(response);
                response.BlockUntilDone();
            }
            else if (message.Data is DataOperation)
            {
                DataOperationResult result = _database.ProcessOperation((DataOperation)message.Data);
                SendMessage(new Message(message, result, false));
            }
            else if (message.Data is DatabaseCreate)
            {
                _database.Create();
                SendMessage(new Message(message, new Acknowledgement(), false));
            }
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
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
                foreach (var def in _controllerNodes)
                {
                    if (!connections.Any(e => Equals(e.Item1, def)))
                    {
                        Logger.Log("Attempting to reconnect to " + def.ConnectionName, LogLevel.Info);

                        Message message = new Message(def, new JoinAttempt(NodeType.Storage, _settings.NodeName, _settings.Port, _settings.ToString()), true)
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
                            if (successData.Data["PrimaryController"].ValueAsBoolean)
                            {
                                Logger.Log("Setting the primary controller to " + message.Address.ConnectionName, LogLevel.Info);
                                Primary = message.Address;
                            }

                            SendMessage(new Message(message.Response, new Acknowledgement(), false));
                        }
                    }
                }
            }
        }
    }
}
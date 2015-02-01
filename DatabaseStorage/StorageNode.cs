using Database.Common;
using Database.Common.Messages;
using System.Collections.Generic;
using System.Threading;

namespace Database.Storage
{
    /// <summary>
    /// Represents a storage node.
    /// </summary>
    public class StorageNode : Node
    {
        /// <summary>
        /// The database of the node.
        /// </summary>
        private Database _database;

        /// <summary>
        /// The settings of the storage node.
        /// </summary>
        private StorageNodeSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageNode"/> class.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        public StorageNode(StorageNodeSettings settings)
            : base(settings.Port)
        {
            _settings = settings;
            _database = new Database();
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
                Connections[nodeDef].ConnectionEstablished(attempt.Type);
                Message response = new Message(message, new JoinSuccess(false), false)
                {
                    Address = nodeDef
                };

                SendMessage(response);
            }
            else if (message.Data is DataOperation)
            {
                DataOperationResult result = _database.ProcessOperation((DataOperation)message.Data);
                SendMessage(new Message(message, result, false));
            }
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
        }
    }
}
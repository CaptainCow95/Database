using Database.Common;
using Database.Common.Messages;
using System.Collections.Generic;
using System.Threading;

namespace Database.Query
{
    /// <summary>
    /// Represents a query node.
    /// </summary>
    public class QueryNode : Node
    {
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
                Message message = new Message(def, new JoinAttempt(NodeType.Query, _settings.NodeName, _settings.Port, _settings.ToString()), true);
                message.SendWithoutConfirmation = true;
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
                    else
                    {
                        // success
                        JoinSuccess successData = (JoinSuccess)message.Response.Data;
                        Connections[def].ConnectionEstablished(NodeType.Controller);
                        if (successData.PrimaryController)
                        {
                            Primary = message.Address;
                        }
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
        protected override void MessageReceived(Message message)
        {
        }
    }
}
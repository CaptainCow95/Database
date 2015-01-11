﻿using Database.Common;
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
                Message message = new Message(def, new JoinAttempt(NodeType.Storage, _settings.NodeName, _settings.Port, _settings.ToString()), true);
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
                            Logger.Log("Setting the primary controller to " + message.Address.ConnectionName, LogLevel.Info);
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
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
        }
    }
}
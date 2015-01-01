using Database.Common;
using Database.Common.Messages;
using System.Collections.Generic;
using System.Threading;

namespace Database.Storage
{
    public class StorageNode : Node
    {
        private NodeDefinition _primaryController;
        private StorageNodeSettings _settings;

        public StorageNode(StorageNodeSettings settings)
            : base(settings.Port)
        {
            _settings = settings;
        }

        public override NodeDefinition Self
        {
            get { return new NodeDefinition(_settings.NodeName, _settings.Port); }
        }

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
                        Logger.Log("Failed to join controllers: " + ((JoinFailure)message.Response.Data).Reason);
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
                            _primaryController = message.Address;
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

        protected override void MessageReceived(Message message)
        {
        }
    }
}
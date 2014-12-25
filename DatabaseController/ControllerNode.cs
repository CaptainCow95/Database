using Database.Common;
using Database.Common.Messages;
using System.Collections.Generic;
using System.Threading;

namespace Database.Controller
{
    public class ControllerNode : Node
    {
        private bool _primary = false;
        private ControllerNodeSettings _settings;

        public ControllerNode(ControllerNodeSettings settings)
            : base(NodeType.Controller, settings.Port)
        {
            _settings = settings;
        }

        public override void Run()
        {
            BeforeStart();

            List<NodeDefinition> controllerNodes = NodeDefinition.ParseConnectionString(_settings.ConnectionString);

            // Find yourself
            NodeDefinition self = null;
            foreach (var def in controllerNodes)
            {
                if (def.IsSelf(_settings.Port))
                {
                    self = def;
                    break;
                }
            }

            if (self == null)
            {
                Logger.Log("Could not find myself in the connection string.");
                AfterStop();
                return;
            }

            foreach (var def in controllerNodes)
            {
                if (def != self)
                {
                    Message message = new Message(def, new JoinAttempt(NodeType.Controller, self.Hostname, self.Port, _settings.ToString()), true);
                    message.SendWithoutConfirmation = true;
                    SendMessage(message);
                    message.BlockUntilDone();

                    if (message.Success)
                    {
                        if (message.Response.Data is JoinFailure)
                        {
                            Logger.Log("Failed to join other controllers: " +
                                       ((JoinFailure)message.Response.Data).Reason);
                            AfterStop();
                            return;
                        }
                        else
                        {
                            // success
                            Connections[def].Status = ConnectionStatus.Connected;
                        }
                    }
                }
            }

            if (controllerNodes.Count == 1)
            {
                _primary = true;
            }

            while (Running)
            {
                Thread.Sleep(1);
            }

            AfterStop();
        }

        protected override void MessageReceived(Message message)
        {
            if (message.Data is JoinAttempt)
            {
                JoinAttempt joinAttemptData = (JoinAttempt)message.Data;
                switch (joinAttemptData.Type)
                {
                    case NodeType.Controller:
                        ControllerNodeSettings joinSettings = new ControllerNodeSettings(joinAttemptData.Settings);
                        if (joinSettings.ConnectionString != _settings.ConnectionString)
                        {
                            SendMessage(new Message(message, new JoinFailure("Connection strings do not match."), false));
                        }
                        else if (joinSettings.MaxChunkItemCount != _settings.MaxChunkItemCount)
                        {
                            SendMessage(new Message(message, new JoinFailure("Max chunk item counts do not match."), false));
                        }
                        else if (joinSettings.MaxChunkSize != _settings.MaxChunkSize)
                        {
                            SendMessage(new Message(message, new JoinFailure("Max chunk sizes do not match."), false));
                        }
                        else if (joinSettings.RedundentNodesPerLocation != _settings.RedundentNodesPerLocation)
                        {
                            SendMessage(new Message(message,
                                new JoinFailure("Redundent nodes per location do not match."), false));
                        }
                        else
                        {
                            NodeDefinition nodeDef = new NodeDefinition(joinAttemptData.Name, joinAttemptData.Port);
                            RenameConnection(message.Address, nodeDef);
                            Message response = new Message(message, new JoinSuccess(_primary), false);
                            response.Address = nodeDef;
                            SendMessage(response);
                            Connections[nodeDef].Status = ConnectionStatus.Connected;
                        }

                        break;

                    case NodeType.Query:
                        break;

                    case NodeType.Storage:
                        break;
                }
            }
        }
    }
}
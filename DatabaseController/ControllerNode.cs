using Database.Common;
using Database.Common.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Database.Controller
{
    /// <summary>
    /// Represents a controller node.
    /// </summary>
    public class ControllerNode : Node
    {
        /// <summary>
        /// A list of the controller nodes contained in the connection string.
        /// </summary>
        private List<NodeDefinition> _controllerNodes;

        /// <summary>
        /// The last message id received from the current primary controller.
        /// </summary>
        private uint _lastPrimaryMessageId = 0;

        /// <summary>
        /// The <see cref="NodeDefinition"/> that defines this node.
        /// </summary>
        private NodeDefinition _self;

        /// <summary>
        /// The settings of the controller node.
        /// </summary>
        private ControllerNodeSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerNode"/> class.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        public ControllerNode(ControllerNodeSettings settings)
            : base(settings.Port)
        {
            _settings = settings;
        }

        /// <inheritdoc />
        public override NodeDefinition Self
        {
            get { return _self; }
        }

        /// <inheritdoc />
        public override void Run()
        {
            BeforeStart();

            _controllerNodes = NodeDefinition.ParseConnectionString(_settings.ConnectionString);

            // Find yourself
            _self = null;
            foreach (var def in _controllerNodes)
            {
                if (def.IsSelf(_settings.Port))
                {
                    _self = def;
                    break;
                }
            }

            if (_self == null)
            {
                Logger.Log("Could not find myself in the connection string.");
                AfterStop();
                return;
            }

            foreach (var def in _controllerNodes)
            {
                if (!Equals(def, Self))
                {
                    Message message = new Message(def, new JoinAttempt(NodeType.Controller, _self.Hostname, _self.Port, _settings.ToString()), true);
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
                            JoinSuccess success = (JoinSuccess)message.Response.Data;
                            Connections[def].ConnectionEstablished(NodeType.Controller);
                            if (success.PrimaryController)
                            {
                                Primary = def;
                            }
                        }
                    }
                }
            }

            if (_controllerNodes.Count == 1)
            {
                Primary = Self;
            }

            while (Running)
            {
                Update();

                int i = 0;
                while (Running && i < 30)
                {
                    Thread.Sleep(1000);
                    ++i;
                }
            }

            AfterStop();
        }

        /// <inheritdoc />
        protected override void MessageReceived(Message message)
        {
            if (Equals(message.Address, Primary))
            {
                _lastPrimaryMessageId = Math.Max(_lastPrimaryMessageId, message.ID);
            }

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
                        else if (joinSettings.RedundantNodesPerLocation != _settings.RedundantNodesPerLocation)
                        {
                            SendMessage(new Message(message,
                                new JoinFailure("Redundent nodes per location do not match."), false));
                        }
                        else
                        {
                            NodeDefinition nodeDef = new NodeDefinition(joinAttemptData.Name, joinAttemptData.Port);
                            RenameConnection(message.Address, nodeDef);
                            Connections[nodeDef].ConnectionEstablished(joinAttemptData.Type);
                            Message response = new Message(message, new JoinSuccess(Equals(Primary, Self)), false);
                            response.Address = nodeDef;
                            SendMessage(response);
                        }

                        break;

                    case NodeType.Query:
                        QueryNodeSettings queryJoinSettings = new QueryNodeSettings(joinAttemptData.Settings);
                        if (queryJoinSettings.ConnectionString != _settings.ConnectionString)
                        {
                            SendMessage(new Message(message, new JoinFailure("Connection strings do not match."), false));
                        }
                        else
                        {
                            NodeDefinition nodeDef = new NodeDefinition(joinAttemptData.Name, joinAttemptData.Port);
                            RenameConnection(message.Address, nodeDef);
                            Connections[nodeDef].ConnectionEstablished(joinAttemptData.Type);
                            Message response = new Message(message, new JoinSuccess(Equals(Primary, Self)), false);
                            response.Address = nodeDef;
                            SendMessage(response);
                        }

                        break;

                    case NodeType.Storage:
                        StorageNodeSettings storageJoinSettings = new StorageNodeSettings(joinAttemptData.Settings);
                        if (storageJoinSettings.ConnectionString != _settings.ConnectionString)
                        {
                            SendMessage(new Message(message, new JoinFailure("Connection strings do not match."), false));
                        }
                        else
                        {
                            NodeDefinition nodeDef = new NodeDefinition(joinAttemptData.Name, joinAttemptData.Port);
                            RenameConnection(message.Address, nodeDef);
                            Connections[nodeDef].ConnectionEstablished(joinAttemptData.Type);
                            Message response = new Message(message, new JoinSuccess(Equals(Primary, Self)), false);
                            response.Address = nodeDef;
                            SendMessage(response);
                        }

                        break;
                }
            }
            else if (message.Data is VotingRequest)
            {
                if (Primary != null)
                {
                    SendMessage(new Message(message, new VotingResponse(false), false));
                }
                else
                {
                    uint max = 0;
                    List<Tuple<NodeDefinition, uint>> votingIds = new List<Tuple<NodeDefinition, uint>>();
                    foreach (var def in _controllerNodes)
                    {
                        if (Equals(def, Self))
                        {
                            continue;
                        }

                        Message idRequest = new Message(def, new LastPrimaryMessageIdRequest(), true);
                        SendMessage(idRequest);
                        idRequest.BlockUntilDone();

                        if (idRequest.Success)
                        {
                            uint votingId = ((LastPrimaryMessageIdResponse)idRequest.Response.Data).LastMessageId;
                            max = Math.Max(max, votingId);
                            votingIds.Add(new Tuple<NodeDefinition, uint>(def, votingId));
                        }
                    }

                    if (votingIds.Count > 0)
                    {
                        var top = votingIds.Where(e => e.Item2 == max).OrderBy(e => e.Item1);

                        if (Equals(top.First().Item1, message.Address))
                        {
                            SendMessage(new Message(message, new VotingResponse(true), false));
                        }
                        else
                        {
                            SendMessage(new Message(message, new VotingResponse(false), false));
                        }
                    }
                    else
                    {
                        SendMessage(new Message(message, new VotingResponse(false), false));
                    }
                }
            }
            else if (message.Data is LastPrimaryMessageIdRequest)
            {
                SendMessage(new Message(message, new LastPrimaryMessageIdResponse(_lastPrimaryMessageId), false));
            }
            else if (message.Data is PrimaryAnnouncement)
            {
                Primary = message.Address;
                _lastPrimaryMessageId = 0;
            }
        }

        /// <summary>
        /// Initiates a voting sequence.
        /// </summary>
        private void InitiateVoting()
        {
            bool becomePrimary = true;

            // start at 1 because GetConnectedNodes doesn't include the current node.
            int controllerActiveCount = 1;
            var connectedNodes = GetConnectedNodes();
            foreach (var def in _controllerNodes)
            {
                if (connectedNodes.Any(e => Equals(e.Item1, def)))
                {
                    controllerActiveCount++;
                }
            }

            if (controllerActiveCount > _controllerNodes.Count / 2)
            {
                bool receivedResponse = false;
                foreach (var def in _controllerNodes)
                {
                    if (Equals(def, Self))
                    {
                        continue;
                    }

                    Message message = new Message(def, new VotingRequest(), true);
                    SendMessage(message);
                    message.BlockUntilDone();
                    if (message.Success)
                    {
                        receivedResponse = true;
                        if (!((VotingResponse)message.Response.Data).Answer)
                        {
                            becomePrimary = false;
                            break;
                        }
                    }
                }

                if (!receivedResponse)
                {
                    becomePrimary = false;
                }
            }
            else
            {
                becomePrimary = false;
            }

            if (becomePrimary)
            {
                Primary = Self;
                foreach (var def in _controllerNodes)
                {
                    if (Equals(def, Self))
                    {
                        continue;
                    }

                    SendMessage(new Message(def, new PrimaryAnnouncement(), false));
                }
            }
        }

        /// <summary>
        /// Updates the controller.
        /// </summary>
        private void Update()
        {
            if (Primary != null && !Equals(Primary, Self) && GetConnectedNodes().All(e => !Equals(e.Item1, Primary)))
            {
                Primary = null;
            }

            if (Primary == null)
            {
                InitiateVoting();
            }
        }
    }
}
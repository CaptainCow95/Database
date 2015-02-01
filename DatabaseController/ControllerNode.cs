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
        /// The thread that handles reconnecting to other controller nodes.
        /// </summary>
        private Thread _updateThread;

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
                Logger.Log("Could not find myself in the connection string, shutting down.", LogLevel.Error);
                AfterStop();
                return;
            }

            foreach (var def in _controllerNodes)
            {
                if (!Equals(def, Self) && !ConnectToController(def))
                {
                    AfterStop();
                    return;
                }
            }

            if (_controllerNodes.Count == 1)
            {
                Logger.Log("Only controller in the network, becoming primary.", LogLevel.Info);
                Primary = Self;
            }

            _updateThread = new Thread(UpdateThreadRun);
            _updateThread.Start();

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
                Logger.Log("Primary controller unreachable, searching for new primary.", LogLevel.Info);
                Primary = null;
            }

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

            if (controllerActiveCount <= _controllerNodes.Count / 2)
            {
                Logger.Log("Not enough connected nodes to remain primary.", LogLevel.Info);
                Primary = null;
            }
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
                            if (Equals(message.Address, nodeDef))
                            {
                                Logger.Log("Duplicate connection found. Not recognizing new connection in favor of the old one.", LogLevel.Info);
                            }

                            RenameConnection(message.Address, nodeDef);
                            Connections[nodeDef].ConnectionEstablished(joinAttemptData.Type);
                            Message response = new Message(message, new JoinSuccess(Equals(Primary, Self)), false)
                            {
                                Address = nodeDef
                            };

                            SendMessage(response);

                            if (joinAttemptData.Primary)
                            {
                                Logger.Log("Connection to primary controller established, setting primary to " + message.Address.ConnectionName, LogLevel.Info);
                                Primary = nodeDef;
                            }
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
                            Message response = new Message(message, new JoinSuccess(Equals(Primary, Self)), false)
                            {
                                Address = nodeDef
                            };

                            SendMessage(response);

                            SendStorageNodeConnectionMessage();
                            SendQueryNodeConnectionMessage();
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
                            Message response = new Message(message, new JoinSuccess(Equals(Primary, Self)), false)
                            {
                                Address = nodeDef
                            };

                            SendMessage(response);

                            SendStorageNodeConnectionMessage();
                        }

                        break;

                    case NodeType.Console:
                        Connections[message.Address].ConnectionEstablished(joinAttemptData.Type);
                        SendMessage(new Message(message, new JoinSuccess(Equals(Primary, Self)), false));
                        break;

                    case NodeType.Api:
                        if (joinAttemptData.Settings != _settings.ConnectionString)
                        {
                            SendMessage(new Message(message, new JoinFailure("Connection strings do not match."), false));
                        }
                        else
                        {
                            Connections[message.Address].ConnectionEstablished(joinAttemptData.Type);
                            SendMessage(new Message(message, new JoinSuccess(Equals(Primary, Self)), false));

                            SendQueryNodeConnectionMessage();
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

                    bool votingResponse = false;
                    if (votingIds.Count > 0)
                    {
                        var top = votingIds.Where(e => e.Item2 == max).OrderBy(e => e.Item1.ConnectionName);

                        if (Equals(top.First().Item1, message.Address))
                        {
                            votingResponse = true;
                        }
                    }

                    SendMessage(new Message(message, new VotingResponse(votingResponse), false));
                }
            }
            else if (message.Data is LastPrimaryMessageIdRequest)
            {
                SendMessage(new Message(message, new LastPrimaryMessageIdResponse(_lastPrimaryMessageId), false));
            }
            else if (message.Data is PrimaryAnnouncement)
            {
                Logger.Log("Setting the primary controller to " + message.Address.ConnectionName, LogLevel.Info);
                Primary = message.Address;
            }
            else if (message.Data is DataOperation)
            {
                var nodes = GetConnectedNodes();
                bool found = false;
                foreach (var node in nodes)
                {
                    if (node.Item2 == NodeType.Query)
                    {
                        found = true;
                        Message op = new Message(node.Item1, message.Data, true);
                        SendMessage(op);

                        op.BlockUntilDone();

                        if (op.Success)
                        {
                            SendMessage(new Message(message, op.Response.Data, false));
                        }
                        else
                        {
                            SendMessage(new Message(message, new DataOperationResult("{\"success\":false,\"error\":\"Message to the query node failed.\"}"), false));
                        }

                        break;
                    }
                }

                if (!found)
                {
                    SendMessage(new Message(message, new DataOperationResult("{\"success\":false,\"error\":\"Could not reach a query node.\"}"), false));
                }
            }
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
            _lastPrimaryMessageId = 0;
        }

        /// <summary>
        /// Connects to a controller.
        /// </summary>
        /// <param name="target">The target controller to try to connect to.</param>
        /// <returns>A value indicating whether the target was connected to.</returns>
        private bool ConnectToController(NodeDefinition target)
        {
            Message message = new Message(target, new JoinAttempt(_self.Hostname, _self.Port, _settings.ToString(), Equals(Primary, Self)), true)
            {
                SendWithoutConfirmation = true
            };

            SendMessage(message);
            message.BlockUntilDone();

            if (message.Success)
            {
                if (message.Response.Data is JoinFailure)
                {
                    Logger.Log("Failed to join other controllers: " + ((JoinFailure)message.Response.Data).Reason, LogLevel.Error);
                    return false;
                }

                // success
                Logger.Log("Connected to controller " + target.ConnectionName, LogLevel.Info);
                JoinSuccess success = (JoinSuccess)message.Response.Data;
                Connections[target].ConnectionEstablished(NodeType.Controller);
                if (success.PrimaryController)
                {
                    Logger.Log("Setting the primary controller to " + target.ConnectionName, LogLevel.Info);
                    Primary = target;
                }
            }
            else
            {
                Logger.Log("Timeout while trying to connect to " + target.ConnectionName, LogLevel.Info);
            }

            return true;
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

                    Message message = new Message(def, new VotingRequest(), true)
                    {
                        // Increase timeout so that there is extra time if a voting id request times out in another controller.
                        ResponseTimeout = 60
                    };

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
                    Logger.Log("Vote failed, no responses received from connected nodes.", LogLevel.Warning);
                    becomePrimary = false;
                }
            }
            else
            {
                Logger.Log("Vote failed, not enough connected controllers for a majority.", LogLevel.Error);
                becomePrimary = false;
            }

            if (becomePrimary)
            {
                if (Primary != null)
                {
                    Logger.Log("Primary discovered during voting, sticking with current primary.", LogLevel.Info);
                }
                else
                {
                    Logger.Log("Vote successful, becoming the primary controller.", LogLevel.Info);
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
        }

        /// <summary>
        /// Sends a <see cref="NodeList"/> message to all active API nodes.
        /// </summary>
        private void SendQueryNodeConnectionMessage()
        {
            // Only do this if you are the primary controller.
            if (!Equals(Self, Primary))
            {
                return;
            }

            var nodes = GetConnectedNodes();
            NodeList data = new NodeList(nodes.Where(e => e.Item2 == NodeType.Query).Select(e => e.Item1.ConnectionName).ToList());

            foreach (var item in GetConnectedNodes())
            {
                if (item.Item2 == NodeType.Api)
                {
                    SendMessage(new Message(item.Item1, data, false));
                }
            }
        }

        /// <summary>
        /// Sends a <see cref="NodeList"/> message to all active query nodes.
        /// </summary>
        private void SendStorageNodeConnectionMessage()
        {
            // Only do this if you are the primary controller.
            if (!Equals(Self, Primary))
            {
                return;
            }

            var nodes = GetConnectedNodes();
            NodeList data = new NodeList(nodes.Where(e => e.Item2 == NodeType.Storage).Select(e => e.Item1.ConnectionName).ToList());

            foreach (var item in GetConnectedNodes())
            {
                if (item.Item2 == NodeType.Query)
                {
                    SendMessage(new Message(item.Item1, data, false));
                }
            }
        }

        /// <summary>
        /// Runs the thread that handles reconnecting to the other controllers.
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
                    if (Equals(def, Self))
                    {
                        continue;
                    }

                    if (!connections.Any(e => Equals(e.Item1, def)))
                    {
                        Logger.Log("Attempting to reconnect to " + def.ConnectionName, LogLevel.Info);
                        ConnectToController(def);
                    }
                }

                if (Primary == null)
                {
                    Logger.Log("Initiating voting.", LogLevel.Info);
                    InitiateVoting();
                }
            }
        }
    }
}
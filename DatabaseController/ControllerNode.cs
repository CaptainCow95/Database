using Database.Common;
using Database.Common.DataOperation;
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
        /// The time between maintenance runs.
        /// </summary>
        private const int MaintenanceRunTime = 60;

        /// <summary>
        /// The lock object to use when balancing.
        /// </summary>
        private readonly object _balancingLockObject = new object();

        /// <summary>
        /// The list of where the various database chunks are located.
        /// </summary>
        private readonly List<ChunkDefinition> _chunkList = new List<ChunkDefinition>();

        /// <summary>
        /// A list of the connected storage nodes and their weights.
        /// </summary>
        private readonly List<Tuple<NodeDefinition, int>> _storageNodes = new List<Tuple<NodeDefinition, int>>();

        /// <summary>
        /// The current balancing state.
        /// </summary>
        private volatile BalancingState _balancing = BalancingState.None;

        /// <summary>
        /// The chunk that is currently being transferred.
        /// </summary>
        private ChunkDefinition _chunkBeingTransfered = null;

        /// <summary>
        /// A list of the controller nodes contained in the connection string.
        /// </summary>
        private List<NodeDefinition> _controllerNodes;

        /// <summary>
        /// The last message id received from the current primary controller.
        /// </summary>
        private uint _lastPrimaryMessageId = 0;

        /// <summary>
        /// The maintenance thread.
        /// </summary>
        private Thread _maintenanceThread;

        /// <summary>
        /// The node that is currently managing a chunk.
        /// </summary>
        private NodeDefinition _nodeManagingChunk = null;

        /// <summary>
        /// The node that is currently transferring a chunk.
        /// </summary>
        private NodeDefinition _nodeTransferingChunk = null;

        /// <summary>
        /// The thread that handles reconnecting to the other controller nodes.
        /// </summary>
        private Thread _reconnectionThread;

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

        /// <summary>
        /// The different balance states of the controller.
        /// </summary>
        private enum BalancingState
        {
            /// <summary>
            /// The controller is currently doing nothing.
            /// </summary>
            None,

            /// <summary>
            /// The controller is currently balancing the chunks.
            /// </summary>
            Balancing,

            /// <summary>
            /// The controller is currently splitting or merging a chunk.
            /// </summary>
            ChunkManagement
        }

        /// <inheritdoc />
        public override NodeDefinition Self
        {
            get { return _self; }
        }

        /// <summary>
        /// Gets a list of the current database chunks.
        /// </summary>
        /// <returns>The list of the current database chunks.</returns>
        public IReadOnlyCollection<ChunkDefinition> GetChunkList()
        {
            lock (_chunkList)
            {
                return _chunkList.ToList().AsReadOnly();
            }
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

            // If you get a JoinFailure from any other node, stop because this node's settings are wrong.
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

            _reconnectionThread = new Thread(ReconnectionThreadRun);
            _reconnectionThread.Start();

            _maintenanceThread = new Thread(MaintenanceThreadRun);
            _maintenanceThread.Start();

            while (Running)
            {
                Thread.Sleep(1);
            }

            AfterStop();
        }

        /// <inheritdoc />
        protected override void ConnectionLost(NodeDefinition node, NodeType type)
        {
            if (type == NodeType.Controller)
            {
                if (Equals(Primary, node))
                {
                    Logger.Log("Primary controller unreachable, searching for new primary.", LogLevel.Info);
                    Primary = null;
                }

                // start at 1 because GetConnectedNodes doesn't include the current node.
                var connectedNodes = GetConnectedNodes();
                int controllerActiveCount = 1 + _controllerNodes.Count(def => connectedNodes.Any(e => Equals(e.Item1, def)));

                if (controllerActiveCount <= _controllerNodes.Count / 2)
                {
                    Logger.Log("Not enough connected nodes to remain primary.", LogLevel.Info);
                    Primary = null;
                }
            }
            else if (type == NodeType.Storage)
            {
                lock (_storageNodes)
                {
                    _storageNodes.RemoveAll(e => e.Item1.Equals(node));
                }

                lock (_chunkList)
                {
                    _chunkList.RemoveAll(e => Equals(e.Node, node));
                }

                lock (_balancingLockObject)
                {
                    if (Equals(node, _nodeManagingChunk))
                    {
                        _balancing = BalancingState.None;
                        _nodeManagingChunk = null;
                    }

                    if (Equals(node, _nodeTransferingChunk))
                    {
                        _balancing = BalancingState.None;
                        _nodeTransferingChunk = null;
                        _chunkBeingTransfered = null;
                        Logger.Log("Chunk transfer failed.", LogLevel.Info);
                    }
                }
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
                HandleJoinAttemptMessage(message, (JoinAttempt)message.Data);
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
            else if (message.Data is ChunkListUpdate)
            {
                lock (_chunkList)
                {
                    _chunkList.Clear();
                    _chunkList.AddRange(((ChunkListUpdate)message.Data).ChunkList);
                }

                SendMessage(new Message(message, new Acknowledgement(), false));
            }
            else if (message.Data is ChunkSplit)
            {
                ChunkSplit splitData = (ChunkSplit)message.Data;
                lock (_chunkList)
                {
                    _chunkList.Remove(_chunkList.Find(e => Equals(e.Start, splitData.Start1)));
                    _chunkList.Add(new ChunkDefinition(splitData.Start1, splitData.End1, message.Address));
                    _chunkList.Add(new ChunkDefinition(splitData.Start2, splitData.End2, message.Address));
                }

                SendMessage(new Message(message, new Acknowledgement(), false));

                lock (_balancingLockObject)
                {
                    _balancing = BalancingState.None;
                    _nodeManagingChunk = null;

                    Logger.Log("Committing chunk split.", LogLevel.Debug);
                }

                SendChunkList();
            }
            else if (message.Data is ChunkMerge)
            {
                ChunkMerge mergeData = (ChunkMerge)message.Data;
                lock (_chunkList)
                {
                    _chunkList.Remove(_chunkList.Find(e => Equals(e.Start, mergeData.Start)));
                    _chunkList.Remove(_chunkList.Find(e => Equals(e.End, mergeData.End)));
                    _chunkList.Add(new ChunkDefinition(mergeData.Start, mergeData.End, message.Address));
                }

                SendMessage(new Message(message, new Acknowledgement(), false));

                lock (_balancingLockObject)
                {
                    _balancing = BalancingState.None;
                    _nodeManagingChunk = null;

                    Logger.Log("Committing chunk merge.", LogLevel.Debug);
                }

                SendChunkList();
            }
            else if (message.Data is ChunkManagementRequest)
            {
                bool success = false;
                lock (_balancingLockObject)
                {
                    if (_balancing == BalancingState.None)
                    {
                        _balancing = BalancingState.ChunkManagement;
                        success = true;
                        _nodeManagingChunk = message.Address;

                        Logger.Log("Granting chunk management request.", LogLevel.Debug);
                    }
                }

                SendMessage(new Message(message, new ChunkManagementResponse(success), false));
            }
            else if (message.Data is ChunkTransferComplete)
            {
                if (((ChunkTransferComplete)message.Data).Success)
                {
                    lock (_chunkList)
                    {
                        int index = _chunkList.IndexOf(_chunkBeingTransfered);
                        _chunkList[index] = new ChunkDefinition(_chunkBeingTransfered.Start, _chunkBeingTransfered.End, message.Address);
                    }

                    Logger.Log("Chunk transfer completed successfully.", LogLevel.Info);
                    SendChunkList();
                    BalanceChunks();
                }
                else
                {
                    Logger.Log("Chunk transfer failed.", LogLevel.Info);

                    lock (_balancingLockObject)
                    {
                        _nodeTransferingChunk = null;
                        _chunkBeingTransfered = null;
                        _balancing = BalancingState.None;
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
            _lastPrimaryMessageId = 0;
        }

        /// <summary>
        /// Balances the chunks on the storage nodes.
        /// </summary>
        private void BalanceChunks()
        {
            SortedList<NodeDefinition, float> chunksPerNode = new SortedList<NodeDefinition, float>();
            lock (_chunkList)
            {
                if (_chunkList.Count == 0)
                {
                    lock (_balancingLockObject)
                    {
                        _nodeTransferingChunk = null;
                        _chunkBeingTransfered = null;
                        _balancing = BalancingState.None;
                        return;
                    }
                }

                foreach (var item in _chunkList)
                {
                    if (!chunksPerNode.ContainsKey(item.Node))
                    {
                        chunksPerNode.Add(item.Node, 1);
                    }
                    else
                    {
                        chunksPerNode[item.Node] += 1;
                    }
                }

                lock (_storageNodes)
                {
                    foreach (var item in _storageNodes)
                    {
                        if (!chunksPerNode.ContainsKey(item.Item1))
                        {
                            chunksPerNode.Add(item.Item1, 0);
                        }

                        chunksPerNode[item.Item1] /= item.Item2;
                    }
                }

                var sortedChunks = chunksPerNode.OrderBy(e => e.Value).ToList();
                if (sortedChunks[sortedChunks.Count - 1].Value - sortedChunks[0].Value > 2)
                {
                    var minNode = sortedChunks[0].Key;
                    var maxNode = sortedChunks[sortedChunks.Count - 1].Key;
                    Random rand = new Random();
                    var possibleChunks = _chunkList.Where(e => Equals(e.Node, maxNode)).ToList();
                    var chunk = possibleChunks[rand.Next(possibleChunks.Count)];
                    _nodeTransferingChunk = minNode;
                    _chunkBeingTransfered = chunk;
                    Logger.Log("Starting chunk transfer.", LogLevel.Info);
                    SendMessage(new Message(minNode, new ChunkTransfer(maxNode, chunk.Start, chunk.End), false));
                }
                else
                {
                    lock (_balancingLockObject)
                    {
                        _nodeTransferingChunk = null;
                        _chunkBeingTransfered = null;
                        _balancing = BalancingState.None;
                    }
                }
            }
        }

        /// <summary>
        /// Called when this node is to become the primary.
        /// </summary>
        private void BecomePrimary()
        {
            lock (_balancingLockObject)
            {
                _balancing = BalancingState.ChunkManagement;
            }

            Primary = Self;
            foreach (var def in GetConnectedNodes().Select(e => e.Item1))
            {
                if (Equals(def, Self))
                {
                    continue;
                }

                SendMessage(new Message(def, new PrimaryAnnouncement(), false));
            }

            SendQueryNodeConnectionMessage();
            SendStorageNodeConnectionMessage();

            foreach (var def in GetConnectedNodes().Where(e => e.Item2 == NodeType.Storage).Select(e => e.Item1))
            {
                Message message = new Message(def, new ChunkListRequest(), true);
                SendMessage(message);
                message.BlockUntilDone();
                if (message.Success)
                {
                    ChunkListResponse response = (ChunkListResponse)message.Response.Data;
                    foreach (var item in response.Chunks)
                    {
                        if (!_chunkList.Any(e => e.Start.Equals(item.Start)))
                        {
                            _chunkList.Add(item);
                        }
                    }
                }
            }

            TryCreateDatabase();

            lock (_balancingLockObject)
            {
                _balancing = BalancingState.None;
            }
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
                Connections[target].ConnectionEstablished(target, NodeType.Controller);
                if (success.Data["PrimaryController"].ValueAsBoolean)
                {
                    Logger.Log("Setting the primary controller to " + target.ConnectionName, LogLevel.Info);
                    Primary = target;
                }

                SendMessage(new Message(message.Response, new Acknowledgement(), false));
            }
            else
            {
                Logger.Log("Timeout while trying to connect to " + target.ConnectionName, LogLevel.Info);
            }

            return true;
        }

        /// <summary>
        /// Handles a <see cref="JoinAttempt"/> message.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="joinAttemptData">The <see cref="JoinAttempt"/> that was received.</param>
        private void HandleJoinAttemptMessage(Message message, JoinAttempt joinAttemptData)
        {
            switch (joinAttemptData.Type)
            {
                case NodeType.Controller:
                    ControllerNodeSettings joinSettings = new ControllerNodeSettings(joinAttemptData.Settings);
                    if (joinSettings.ConnectionString != _settings.ConnectionString)
                    {
                        SendMessage(new Message(message, new JoinFailure("Connection strings do not match."), false)
                        {
                            SendWithoutConfirmation = true
                        });
                    }
                    else if (joinSettings.MaxChunkItemCount != _settings.MaxChunkItemCount)
                    {
                        SendMessage(new Message(message, new JoinFailure("Max chunk item counts do not match."), false)
                        {
                            SendWithoutConfirmation = true
                        });
                    }
                    else if (joinSettings.RedundantNodesPerLocation != _settings.RedundantNodesPerLocation)
                    {
                        SendMessage(new Message(message, new JoinFailure("Redundent nodes per location do not match."), false)
                        {
                            SendWithoutConfirmation = true
                        });
                    }
                    else
                    {
                        NodeDefinition nodeDef = new NodeDefinition(joinAttemptData.Name, joinAttemptData.Port);
                        if (Equals(message.Address, nodeDef))
                        {
                            Logger.Log("Duplicate connection found. Not recognizing new connection in favor of the old one.", LogLevel.Info);
                        }

                        RenameConnection(message.Address, nodeDef);
                        Connections[nodeDef].ConnectionEstablished(nodeDef, joinAttemptData.Type);
                        Message response = new Message(message, new JoinSuccess(new Document("{\"PrimaryController\":" + Equals(Primary, Self).ToString().ToLower() + "}")), true)
                        {
                            Address = nodeDef
                        };

                        SendMessage(response);
                        response.BlockUntilDone();

                        if (response.Success)
                        {
                            if (joinAttemptData.Primary)
                            {
                                Logger.Log("Connection to primary controller established, setting primary to " + message.Address.ConnectionName, LogLevel.Info);
                                Primary = nodeDef;
                            }

                            SendChunkList();
                        }
                    }

                    break;

                case NodeType.Query:
                    QueryNodeSettings queryJoinSettings = new QueryNodeSettings(joinAttemptData.Settings);
                    if (queryJoinSettings.ConnectionString != _settings.ConnectionString)
                    {
                        SendMessage(new Message(message, new JoinFailure("Connection strings do not match."), false)
                        {
                            SendWithoutConfirmation = true
                        });
                    }
                    else
                    {
                        NodeDefinition nodeDef = new NodeDefinition(joinAttemptData.Name, joinAttemptData.Port);
                        RenameConnection(message.Address, nodeDef);
                        Connections[nodeDef].ConnectionEstablished(nodeDef, joinAttemptData.Type);
                        Message response = new Message(message, new JoinSuccess(new Document("{\"PrimaryController\":" + Equals(Primary, Self).ToString().ToLower() + "}")),
                            true)
                        {
                            Address = nodeDef
                        };

                        SendMessage(response);
                        response.BlockUntilDone();

                        if (response.Success)
                        {
                            SendStorageNodeConnectionMessage();
                            SendQueryNodeConnectionMessage();

                            SendChunkList();
                        }
                    }

                    break;

                case NodeType.Storage:
                    StorageNodeSettings storageJoinSettings = new StorageNodeSettings(joinAttemptData.Settings);
                    if (storageJoinSettings.ConnectionString != _settings.ConnectionString)
                    {
                        SendMessage(new Message(message, new JoinFailure("Connection strings do not match."), false)
                        {
                            SendWithoutConfirmation = true
                        });
                    }
                    else
                    {
                        NodeDefinition nodeDef = new NodeDefinition(joinAttemptData.Name, joinAttemptData.Port);
                        RenameConnection(message.Address, nodeDef);
                        Connections[nodeDef].ConnectionEstablished(nodeDef, joinAttemptData.Type);

                        var responseData = new Document();
                        responseData["PrimaryController"] = new DocumentEntry("PrimaryController", DocumentEntryType.Boolean, Equals(Primary, Self));
                        responseData["MaxChunkItemCount"] = new DocumentEntry("MaxChunkItemCount", DocumentEntryType.Integer, _settings.MaxChunkItemCount);

                        Message response = new Message(message, new JoinSuccess(responseData), true)
                        {
                            Address = nodeDef
                        };

                        SendMessage(response);
                        response.BlockUntilDone();

                        if (response.Success)
                        {
                            lock (_storageNodes)
                            {
                                _storageNodes.Add(new Tuple<NodeDefinition, int>(nodeDef, storageJoinSettings.Weight));
                            }

                            SendStorageNodeConnectionMessage();

                            TryCreateDatabase();
                        }
                    }

                    break;

                case NodeType.Api:
                    if (joinAttemptData.Settings != _settings.ConnectionString)
                    {
                        SendMessage(new Message(message, new JoinFailure("Connection strings do not match."), false)
                        {
                            SendWithoutConfirmation = true
                        });
                    }
                    else
                    {
                        Connections[message.Address].ConnectionEstablished(message.Address, joinAttemptData.Type);
                        var apiResponse = new Message(message, new JoinSuccess(new Document("{\"PrimaryController\":" + Equals(Primary, Self).ToString().ToLower() + "}")), true);
                        SendMessage(apiResponse);
                        apiResponse.BlockUntilDone();

                        if (apiResponse.Success)
                        {
                            SendQueryNodeConnectionMessage();
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Initiates a voting sequence.
        /// </summary>
        private void InitiateVoting()
        {
            bool becomePrimary = true;

            // start at 1 because GetConnectedNodes doesn't include the current node.
            var connectedNodes = GetConnectedNodes();
            int controllerActiveCount = 1 + _controllerNodes.Count(def => connectedNodes.Any(e => Equals(e.Item1, def)));

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
                    BecomePrimary();
                }
            }
        }

        /// <summary>
        /// The run method for the maintenance thread.
        /// </summary>
        private void MaintenanceThreadRun()
        {
            for (int i = 0; i < MaintenanceRunTime && Running; ++i)
            {
                Thread.Sleep(1000);
            }

            while (Running)
            {
                bool balance = false;
                if (Equals(Primary, Self))
                {
                    // Only balance if you are the primary controller.
                    lock (_balancingLockObject)
                    {
                        if (_balancing == BalancingState.None)
                        {
                            _balancing = BalancingState.Balancing;
                            balance = true;
                        }
                    }
                }

                float difference = 0;
                if (balance)
                {
                    lock (_chunkList)
                    {
                        if (_chunkList.Count > 0)
                        {
                            SortedList<NodeDefinition, float> chunksPerNode = new SortedList<NodeDefinition, float>();
                            foreach (var item in _chunkList)
                            {
                                if (!chunksPerNode.ContainsKey(item.Node))
                                {
                                    chunksPerNode.Add(item.Node, 1);
                                }
                                else
                                {
                                    chunksPerNode[item.Node] += 1;
                                }
                            }

                            lock (_storageNodes)
                            {
                                foreach (var item in _storageNodes)
                                {
                                    if (!chunksPerNode.ContainsKey(item.Item1))
                                    {
                                        chunksPerNode.Add(item.Item1, 0);
                                    }

                                    chunksPerNode[item.Item1] /= item.Item2;
                                }
                            }

                            var sortedChunks = chunksPerNode.OrderBy(e => e.Value).ToList();
                            difference = sortedChunks[sortedChunks.Count - 1].Value - sortedChunks[0].Value;
                        }
                    }
                }

                if (balance && difference >= 5)
                {
                    BalanceChunks();
                }
                else
                {
                    _balancing = BalancingState.None;
                }

                for (int i = 0; i < MaintenanceRunTime && Running; ++i)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// Runs the thread that handles reconnecting to the other controllers.
        /// </summary>
        private void ReconnectionThreadRun()
        {
            Random rand = new Random();

            int timeToWait = rand.Next(30, 60);
            int i = 0;
            while (i < timeToWait && Running)
            {
                Thread.Sleep(1000);
                ++i;
            }

            while (Running)
            {
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

                timeToWait = rand.Next(30, 60);
                i = 0;
                while (i < timeToWait && Running)
                {
                    Thread.Sleep(1000);
                    ++i;
                }
            }
        }

        /// <summary>
        /// Sends a <see cref="ChunkListUpdate"/> message to all connected nodes.
        /// </summary>
        private void SendChunkList()
        {
            if (!Equals(Self, Primary))
            {
                return;
            }

            lock (_chunkList)
            {
                var update = new ChunkListUpdate(_chunkList);

                foreach (var node in GetConnectedNodes().Where(e => e.Item2 == NodeType.Controller || e.Item2 == NodeType.Query))
                {
                    Message message = new Message(node.Item1, update, true);
                    SendMessage(message);
                    message.BlockUntilDone();

                    if (!message.Success)
                    {
                        try
                        {
                            Connections[node.Item1].Disconnect();
                        }
                        catch
                        {
                            // The node may have been removed in the time being, do nothing if so.
                        }
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
        /// Tries to create the database if it doesn't already exist.
        /// </summary>
        private void TryCreateDatabase()
        {
            bool success = false;
            lock (_chunkList)
            {
                if (Equals(Primary, Self) && _chunkList.Count == 0)
                {
                    foreach (var storageNode in GetConnectedNodes().Where(e => e.Item2 == NodeType.Storage).Select(e => e.Item1))
                    {
                        Message storageNodeMessage = new Message(storageNode, new DatabaseCreate(), true);
                        SendMessage(storageNodeMessage);
                        storageNodeMessage.BlockUntilDone();
                        success = storageNodeMessage.Success;
                        if (success)
                        {
                            _chunkList.Add(new ChunkDefinition(new ChunkMarker(ChunkMarkerType.Start), new ChunkMarker(ChunkMarkerType.End), storageNode));
                            break;
                        }
                    }

                    if (!success)
                    {
                        _chunkList.Clear();
                    }
                }
            }

            if (success)
            {
                SendChunkList();
            }
        }
    }
}
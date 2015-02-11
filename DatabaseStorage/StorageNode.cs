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
        /// The database of the node.
        /// </summary>
        private readonly Database _database;

        /// <summary>
        /// A list of the controller nodes.
        /// </summary>
        private List<NodeDefinition> _controllerNodes;

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
                if (attempt.Type != NodeType.Query && attempt.Type != NodeType.Storage)
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
                DataOperationResult result;
                try
                {
                    result = _database.ProcessOperation((DataOperation)message.Data);
                }
                catch (ChunkMovedException)
                {
                    result = new DataOperationResult(ErrorCodes.ChunkMoved, "The chunk has been moved.");
                }
                catch (Exception e)
                {
                    Logger.Log(e.Message + "\nStackTrace:" + e.StackTrace, LogLevel.Error);
                    result = new DataOperationResult(ErrorCodes.FailedMessage, "An exception occurred while processing the operation.");
                }

                SendMessage(new Message(message, result, false));
            }
            else if (message.Data is DatabaseCreate)
            {
                _database.Create();
                SendMessage(new Message(message, new Acknowledgement(), false));
            }
            else if (message.Data is ChunkTransfer)
            {
                var transfer = (ChunkTransfer)message.Data;
                MessageReceivedThreadPool.QueueWorkItem(TransferChunk, transfer);
            }
            else if (message.Data is ChunkDataRequest)
            {
                _database.ProcessChunkDataRequest((ChunkDataRequest)message.Data, message);
            }
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
        }

        /// <summary>
        /// Processes a chunk transfer.
        /// </summary>
        /// <param name="transfer">The chunk to transfer.</param>
        private void TransferChunk(ChunkTransfer transfer)
        {
            Logger.Log("Attempting transfer of chunk " + transfer.Start + " - " + transfer.End + " from " + transfer.Node.ConnectionName, LogLevel.Info);
            Message joinAttempt = new Message(transfer.Node, new JoinAttempt(NodeType.Storage, _settings.NodeName, _settings.Port, _settings.ConnectionString), true)
            {
                SendWithoutConfirmation = true
            };
            SendMessage(joinAttempt);
            joinAttempt.BlockUntilDone();
            if (!joinAttempt.Success)
            {
                Logger.Log("Failed to reach the storage node at " + transfer.Node.ConnectionName, LogLevel.Warning);
                SendMessage(new Message(Primary, new ChunkTransferComplete(false), false));
                return;
            }

            if (joinAttempt.Response.Data is JoinFailure)
            {
                Logger.Log("The storage node at " + transfer.Node.ConnectionName + " denied the join request in order to transfer a chunk.", LogLevel.Error);
                SendMessage(new Message(Primary, new ChunkTransferComplete(false), false));
                return;
            }

            Connections[transfer.Node].ConnectionEstablished(transfer.Node, NodeType.Storage);
            SendMessage(new Message(joinAttempt.Response, new Acknowledgement(), false));

            Message request = new Message(transfer.Node, new ChunkDataRequest(transfer.Start, transfer.End), true);
            SendMessage(request);
            request.BlockUntilDone();
            if (!request.Success)
            {
                Logger.Log("Failed to reach the storage node at " + transfer.Node.ConnectionName, LogLevel.Warning);
                SendMessage(new Message(Primary, new ChunkTransferComplete(false), false));
                return;
            }

            var response = (ChunkDataResponse)request.Response.Data;
            _database.ProcessChunkDataResponse(transfer.Start, transfer.End, response.Data);
            Message ack = new Message(request.Response, new Acknowledgement(), true);
            SendMessage(ack);
            ack.BlockUntilDone();
            try
            {
                Connections[transfer.Node].Disconnect();
                while (Connections.Any(e => Equals(e.Key, transfer.Node)))
                {
                    Thread.Sleep(10);
                }
            }
            catch
            {
                // Probably disconnected in the meantime, don't worry about it.
            }

            SendMessage(new Message(Primary, new ChunkTransferComplete(true), false));
            Logger.Log("Chunk transfer complete.", LogLevel.Info);
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
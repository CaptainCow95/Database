using Database.Common;
using Database.Common.DataOperation;
using Database.Common.Messages;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Database.Storage
{
    /// <summary>
    /// Represents a database.
    /// </summary>
    public class Database
    {
        /// <summary>
        /// The number of seconds between maintenance run cycles.
        /// </summary>
        private const int MaintenanceRunTime = 10;

        /// <summary>
        /// The chunks contained by this database.
        /// </summary>
        private readonly List<DatabaseChunk> _chunks = new List<DatabaseChunk>();

        /// <summary>
        /// The lock to use when accessing the chunks list.
        /// </summary>
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        /// <summary>
        /// The storage node to send messages to.
        /// </summary>
        private readonly StorageNode _node;

        /// <summary>
        /// A value indicating whether to check for a merge during the next maintenance run.
        /// </summary>
        private volatile bool _checkForMerge = false;

        /// <summary>
        /// A value indicating whether to check for a split during the next maintenance run.
        /// </summary>
        private volatile bool _checkForSplit = false;

        /// <summary>
        /// The maximum number of items per chunk before a split occurs.
        /// </summary>
        private int _maxChunkItemCount = ControllerNodeSettings.MaxChunkItemCountDefault;

        /// <summary>
        /// A value indicating whether the maintenance thread is running.
        /// </summary>
        private bool _running = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="node">The storage node to send messages to.</param>
        public Database(StorageNode node)
        {
            _node = node;
            var chunkMaintenanceThread = new Thread(RunMaintenanceThread);
            chunkMaintenanceThread.Start();
        }

        /// <summary>
        /// Creates a database is it doesn't already exist.
        /// </summary>
        public void Create()
        {
            _lock.EnterWriteLock();

            if (_chunks.Count == 0)
            {
                _chunks.Add(new DatabaseChunk(new ChunkMarker(ChunkMarkerType.Start), new ChunkMarker(ChunkMarkerType.End)));
            }

            _lock.ExitWriteLock();
        }

        /// <summary>
        /// Processes a <see cref="ChunkDataRequest"/> message.
        /// </summary>
        /// <param name="request">The request's data.</param>
        /// <param name="requestMessage">The original message.</param>
        public void ProcessChunkDataRequest(ChunkDataRequest request, Message requestMessage)
        {
            Logger.Log("Fulfilling chunk move request.", LogLevel.Info);
            _lock.EnterWriteLock();

            Stopwatch timer = new Stopwatch();
            timer.Start();
            Document data = new Document();
            var chunk = _chunks.Find(e => Equals(e.Start, request.Start) && Equals(e.End, request.End));
            var documents = chunk.Query(new List<QueryItem>());
            for (int i = 0; i < documents.Count; ++i)
            {
                data[i.ToString()] = new DocumentEntry(i.ToString(), DocumentEntryType.Document, documents[i]);
            }

            data["count"] = new DocumentEntry("count", DocumentEntryType.Integer, documents.Count);
            Message response = new Message(requestMessage, new ChunkDataResponse(data), true);
            timer.Stop();
            Logger.Log("Chunk data retrieved in " + timer.Elapsed.TotalSeconds + " seconds.", LogLevel.Info);
            _node.SendDatabaseMessage(response);
            response.BlockUntilDone();
            if (response.Success)
            {
                _node.SendDatabaseMessage(new Message(response.Response, new Acknowledgement(), false));
                _chunks.Remove(chunk);
                Logger.Log("Chunk move request succeeded.", LogLevel.Info);
            }
            else
            {
                Logger.Log("Chunk move request failed.", LogLevel.Info);
            }

            _lock.ExitWriteLock();
        }

        /// <summary>
        /// Processes a <see cref="ChunkDataResponse"/> message.
        /// </summary>
        /// <param name="start">The start of the chunk.</param>
        /// <param name="end">The end of the chunk.</param>
        /// <param name="data">The data that is contained in that chunk.</param>
        public void ProcessChunkDataResponse(ChunkMarker start, ChunkMarker end, Document data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            DatabaseChunk chunk = new DatabaseChunk(start, end);
            for (int i = 0; i < data["count"].ValueAsInteger; ++i)
            {
                var doc = data[i.ToString()].ValueAsDocument;
                chunk.TryAdd(new ObjectId(doc["id"].ValueAsString), doc);
            }

            timer.Stop();

            _lock.EnterWriteLock();

            _chunks.Add(chunk);
            _chunks.Sort();

            _lock.ExitWriteLock();
            Logger.Log("Chunk data added in " + timer.Elapsed.TotalSeconds + " seconds.", LogLevel.Info);
        }

        /// <summary>
        /// Processes a <see cref="ChunkListRequest"/> message.
        /// </summary>
        /// <param name="message">The message to response to.</param>
        /// <param name="request">The <see cref="ChunkListRequest"/> data.</param>
        public void ProcessChunkListRequest(Message message, ChunkListRequest request)
        {
            List<ChunkDefinition> defs = new List<ChunkDefinition>();
            _lock.EnterReadLock();
            defs.AddRange(_chunks.Select(e => new ChunkDefinition(e.Start, e.End, _node.Self)));
            _lock.ExitReadLock();
            ChunkListResponse response = new ChunkListResponse(defs);
            _node.SendDatabaseMessage(new Message(message, response, false));
        }

        /// <summary>
        /// Processes an operation.
        /// </summary>
        /// <param name="operation">The message representing the operation.</param>
        /// <returns>The result of the operation.</returns>
        public DataOperationResult ProcessOperation(DataOperation operation)
        {
            Document doc = new Document(operation.Json);
            if (!doc.Valid)
            {
                return new DataOperationResult(ErrorCodes.InvalidDocument, "The document is invalid.");
            }

            if (doc.Count != 1)
            {
                return new DataOperationResult(ErrorCodes.InvalidDocument, "Multiple entries in the operation.");
            }

            var item = doc.First();
            switch (item.Key)
            {
                case "add":
                    if (doc.CheckForSubkeys())
                    {
                        return new DataOperationResult(ErrorCodes.SubkeysNotAllowed, "Subkeys are not allowed in the add operation.");
                    }

                    return ProcessAddOperation(item.Value.ValueAsDocument);

                case "update":
                    if (doc.CheckForSubkeys())
                    {
                        return new DataOperationResult(ErrorCodes.SubkeysNotAllowed, "Subkeys are not allowed in the update operation.");
                    }

                    return ProcessUpdateOperation(item.Value.ValueAsDocument);

                case "remove":
                    if (doc.CheckForSubkeys())
                    {
                        return new DataOperationResult(ErrorCodes.SubkeysNotAllowed, "Subkeys are not allowed in the remove operation.");
                    }

                    return ProcessRemoveOperation(item.Value.ValueAsDocument);

                case "query":
                    return ProcessQueryOperation(item.Value.ValueAsDocument);

                default:
                    return new DataOperationResult(ErrorCodes.InvalidDocument, "Invalid operation specified.");
            }
        }

        /// <summary>
        /// Sets the maximum number of items per chunk before a split occurs.
        /// </summary>
        /// <param name="maxChunkItemCount">The maximum number of items per chunk before a split occurs.</param>
        public void SetMaxChunkItemCount(int maxChunkItemCount)
        {
            _maxChunkItemCount = maxChunkItemCount;
        }

        /// <summary>
        /// Shuts down the database.
        /// </summary>
        public void Shutdown()
        {
            _running = false;
        }

        /// <summary>
        /// Attempts to merge the database chunks.
        /// </summary>
        private void AttemptMerge()
        {
            BaseMessageData message = null;
            _checkForMerge = false;

            _lock.EnterUpgradeableReadLock();

            for (int i = 0; i < _chunks.Count - 1; ++i)
            {
                if (_chunks[i].Count + _chunks[i + 1].Count < _maxChunkItemCount / 2 &&
                    Equals(_chunks[i].End, _chunks[i + 1].Start))
                {
                    _checkForMerge = true;

                    if (_node.Primary == null)
                    {
                        break;
                    }

                    Message canMerge = new Message(_node.Primary, new ChunkManagementRequest(), true);
                    _node.SendDatabaseMessage(canMerge);
                    canMerge.BlockUntilDone();
                    if (canMerge.Success && ((ChunkManagementResponse)canMerge.Response.Data).Result)
                    {
                        _lock.EnterWriteLock();

                        // Merging two chunks, do the merge and then alert the primary controller before doing anything else.
                        _chunks[i].Merge(_chunks[i + 1]);
                        _chunks.RemoveAt(i + 1);
                        message = new ChunkMerge(_chunks[i].Start, _chunks[i].End);

                        _lock.ExitWriteLock();
                    }

                    break;
                }
            }

            _lock.ExitUpgradeableReadLock();

            if (message != null)
            {
                Message sentMessage = new Message(_node.Primary, message, true);
                _node.SendDatabaseMessage(sentMessage);
                sentMessage.BlockUntilDone();
            }
        }

        /// <summary>
        /// Attempts to split the database chunks.
        /// </summary>
        private void AttemptSplit()
        {
            BaseMessageData message = null;
            _checkForSplit = false;

            _lock.EnterUpgradeableReadLock();

            for (int i = 0; i < _chunks.Count; ++i)
            {
                if (_chunks[i].Count > _maxChunkItemCount)
                {
                    _checkForSplit = true;
                    if (_node.Primary == null)
                    {
                        break;
                    }

                    Message canSplit = new Message(_node.Primary, new ChunkManagementRequest(), true);
                    _node.SendDatabaseMessage(canSplit);
                    canSplit.BlockUntilDone();
                    if (canSplit.Success && ((ChunkManagementResponse)canSplit.Response.Data).Result)
                    {
                        _lock.EnterWriteLock();

                        // Spliting a chunk, do the split and then alert the primary controller before doing anything else.
                        _chunks.Insert(i + 1, _chunks[i].Split());
                        message = new ChunkSplit(_chunks[i].Start, _chunks[i].End, _chunks[i + 1].Start, _chunks[i + 1].End);

                        _lock.ExitWriteLock();
                    }

                    break;
                }
            }

            _lock.ExitUpgradeableReadLock();

            if (message != null)
            {
                Message sentMessage = new Message(_node.Primary, message, true);
                _node.SendDatabaseMessage(sentMessage);
                sentMessage.BlockUntilDone();
            }
        }

        /// <summary>
        /// Builds up the query items for querying the document.
        /// </summary>
        /// <param name="doc">The document to build the queries from.</param>
        /// <returns>A list of the query items for querying the document.</returns>
        private List<QueryItem> BuildQueryItems(Document doc)
        {
            return doc.Select(item => new QueryItem(item.Value)).ToList();
        }

        /// <summary>
        /// Gets the chunk the specified id belongs in.
        /// </summary>
        /// <param name="id">The id to search for.</param>
        /// <returns>The chunk the specified id belongs in.</returns>
        private DatabaseChunk GetChunk(ObjectId id)
        {
            var value = _chunks.SingleOrDefault(e => ChunkMarker.IsBetween(e.Start, e.End, id.ToString()));
            if (value == null)
            {
                _lock.ExitReadLock();
                throw new ChunkMovedException();
            }

            return value;
        }

        /// <summary>
        /// Processes an add operation.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessAddOperation(Document doc)
        {
            AddOperation op = new AddOperation(doc);

            if (!op.Valid)
            {
                return op.ErrorMessage;
            }

            _lock.EnterReadLock();

            bool success = GetChunk(op.Id).TryAdd(op.Id, op.Document);

            _lock.ExitReadLock();

            if (!success)
            {
                return new DataOperationResult(ErrorCodes.InvalidId, "A key with the specified ObjectId already exists.");
            }

            _checkForSplit = true;
            return new DataOperationResult(op.Document);
        }

        /// <summary>
        /// Processes an query operation.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessQueryOperation(Document doc)
        {
            QueryOperation op = new QueryOperation(doc);

            if (!op.Valid)
            {
                return op.ErrorMessage;
            }

            List<QueryItem> queryItems = BuildQueryItems(op.Fields);

            _lock.EnterReadLock();

            List<Document> results = new List<Document>();
            foreach (var item in _chunks)
            {
                results.AddRange(item.Query(queryItems));
            }

            _lock.ExitReadLock();

            StringBuilder builder = new StringBuilder();
            builder.Append("{");

            int count = 0;
            foreach (var result in results)
            {
                builder.Append("\"" + count + "\":");
                builder.Append(result.ToJson());
                builder.Append(",");
                ++count;
            }

            builder.Append("\"count\":" + count);

            builder.Append("}");
            return new DataOperationResult(new Document(builder.ToString()));
        }

        /// <summary>
        /// Processes an remove operation.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessRemoveOperation(Document doc)
        {
            RemoveOperation op = new RemoveOperation(doc);

            if (!op.Valid)
            {
                return op.ErrorMessage;
            }

            _lock.EnterReadLock();

            Document removed;
            bool success = GetChunk(op.DocumentId).TryRemove(op.DocumentId, out removed);

            _lock.ExitReadLock();

            if (success)
            {
                _checkForMerge = true;
            }

            return new DataOperationResult(removed);
        }

        /// <summary>
        /// Processes an update operation.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessUpdateOperation(Document doc)
        {
            UpdateOperation op = new UpdateOperation(doc);
            if (!op.Valid)
            {
                return op.ErrorMessage;
            }

            _lock.EnterReadLock();

            var chunk = GetChunk(op.DocumentId);

            Document value;
            bool found = chunk.TryGet(op.DocumentId, out value);

            if (!found)
            {
                _lock.ExitReadLock();
                return new DataOperationResult(ErrorCodes.InvalidId, "The specified document was not found.");
            }

            var toUpdate = new Document(value);
            toUpdate.Merge(op.UpdateFields);
            foreach (var field in op.RemoveFields)
            {
                toUpdate.RemoveField(field);
            }

            var success = chunk.TryUpdate(op.DocumentId, toUpdate, value);

            _lock.ExitReadLock();

            if (!success)
            {
                return new DataOperationResult(ErrorCodes.InvalidId, "The specified document was not found.");
            }

            _checkForSplit = true;
            _checkForMerge = true;
            return new DataOperationResult(toUpdate);
        }

        /// <summary>
        /// Runs the maintenance thread that checks if a split or merge is needed.
        /// </summary>
        private void RunMaintenanceThread()
        {
            while (_running)
            {
                if (_checkForMerge)
                {
                    AttemptMerge();
                }

                if (_checkForSplit)
                {
                    AttemptSplit();
                }

                if (!_checkForMerge && !_checkForSplit)
                {
                    // No split or merge occurred, so sleep until the next run.
                    for (int i = 0; i < MaintenanceRunTime && _running; ++i)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
        }
    }
}
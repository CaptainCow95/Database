using Database.Common;
using Database.Common.DataOperation;
using Database.Common.Messages;
using System;
using System.Collections.Generic;
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
        public Database()
        {
            var chunkMaintenanceThread = new Thread(RunMaintenanceThread);
            chunkMaintenanceThread.Start();
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
                return new DataOperationResult(ErrorCodes.MultipleOperations, "Must have one and only one operation at a time.");
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
        /// Resets the chunk list if any changes are needed.
        /// </summary>
        /// <param name="chunks">The new chunk list.</param>
        public void ResetChunkList(List<Tuple<ChunkMarker, ChunkMarker>> chunks)
        {
            _lock.EnterWriteLock();

            foreach (var item in chunks)
            {
                if (!_chunks.Any(e => Equals(e.Start, item.Item1) && Equals(e.End, item.Item2)))
                {
                    _chunks.Add(new DatabaseChunk(item.Item1, item.Item2));
                }
            }

            var chunksToRemove = _chunks.Where(item => !chunks.Any(e => Equals(e.Item1, item.Start) && Equals(e.Item2, item.End))).ToList();
            foreach (var item in chunksToRemove)
            {
                _chunks.Remove(item);
            }

            _lock.ExitWriteLock();
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
            return _chunks.Single(e => ChunkMarker.IsBetween(e.Start, e.End, id.ToString()));
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
                    _checkForMerge = false;

                    _lock.EnterUpgradeableReadLock();

                    for (int i = 0; i < _chunks.Count - 1; ++i)
                    {
                        if (_chunks[i].Count + _chunks[i + 1].Count < _maxChunkItemCount / 2)
                        {
                            _lock.EnterWriteLock();

                            _chunks[i].Merge(_chunks[i + 1]);
                            _chunks.RemoveAt(i + 1);
                            --i;

                            _lock.ExitWriteLock();
                        }
                    }

                    _lock.ExitUpgradeableReadLock();
                }

                if (_checkForSplit)
                {
                    _checkForSplit = false;

                    _lock.EnterUpgradeableReadLock();

                    for (int i = 0; i < _chunks.Count; ++i)
                    {
                        if (_chunks[i].Count > _maxChunkItemCount)
                        {
                            _lock.EnterWriteLock();

                            _chunks.Insert(i + 1, _chunks[i].Split());
                            --i;

                            _lock.ExitWriteLock();
                        }
                    }

                    _lock.ExitUpgradeableReadLock();
                }

                for (int i = 0; i < MaintenanceRunTime && _running; ++i)
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
using Database.Common.DataOperation;
using Database.Common.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Database.Storage
{
    /// <summary>
    /// Represents a database.
    /// </summary>
    public class Database
    {
        /// <summary>
        /// The chunks contained by this database.
        /// </summary>
        private readonly List<DatabaseChunk> _chunks = new List<DatabaseChunk>();

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
            lock (_chunks)
            {
                foreach (var item in chunks)
                {
                    if (!_chunks.Any(e => Equals(e.Start, item.Item1) && Equals(e.End, item.Item2)))
                    {
                        _chunks.Add(new DatabaseChunk(item.Item1, item.Item2));
                    }
                }

                var chunksToRemove = new List<DatabaseChunk>();
                foreach (var item in _chunks)
                {
                    if (!chunks.Any(e => Equals(e.Item1, item.Start) && Equals(e.Item2, item.End)))
                    {
                        chunksToRemove.Add(item);
                    }
                }

                _chunks.RemoveAll(e => chunksToRemove.Contains(e));
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
            lock (_chunks)
            {
                return _chunks.Single(e => ChunkMarker.IsBetween(e.Start, e.End, id.ToString()));
            }
        }

        /// <summary>
        /// Processes an add operation.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessAddOperation(Document doc)
        {
            AddOperation op = new AddOperation(doc);

            if (op.Valid)
            {
                // Check if the document already has an id assigned.
                if (op.Document.ContainsKey("id"))
                {
                    // Make sure the id is an actual object id. (Just needs to be a 20 digit hex number)
                    if (op.Document["id"].ValueType == DocumentEntryType.String)
                    {
                        ObjectId id;
                        try
                        {
                            id = new ObjectId(op.Document["id"].ValueAsString);
                        }
                        catch (Exception)
                        {
                            return new DataOperationResult(ErrorCodes.InvalidId, "Document contains an id field that is not an ObjectId.");
                        }

                        bool success = false;
                        lock (_chunks)
                        {
                            if (GetChunk(id).Add(id, op.Document))
                            {
                                success = true;
                            }
                        }

                        if (success)
                        {
                            return new DataOperationResult(op.Document);
                        }

                        return new DataOperationResult(ErrorCodes.InvalidId, "A key with the specified ObjectId already exists.");
                    }

                    return new DataOperationResult(ErrorCodes.InvalidId, "Document contains an id field that is not an ObjectId.");
                }

                throw new Exception("No id in the document, there is something wrong with the network synchronization.");
            }

            return new DataOperationResult(ErrorCodes.InvalidDocument, "Syntax error while trying to parse the add command.");
        }

        /// <summary>
        /// Processes an query operation.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessQueryOperation(Document doc)
        {
            QueryOperation op = new QueryOperation(doc);

            if (op.Valid)
            {
                List<QueryItem> queryItems = BuildQueryItems(op.Fields);

                List<Document> results = new List<Document>();
                lock (_chunks)
                {
                    foreach (var item in _chunks)
                    {
                        results.AddRange(item.Query(queryItems));
                    }
                }

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

            return new DataOperationResult(ErrorCodes.InvalidDocument, "Syntax error while trying to parse the query command.");
        }

        /// <summary>
        /// Processes an remove operation.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessRemoveOperation(Document doc)
        {
            RemoveOperation op = new RemoveOperation(doc);

            if (op.Valid)
            {
                lock (_chunks)
                {
                    GetChunk(op.DocumentId).Remove(op.DocumentId);
                }

                return new DataOperationResult(new Document());
            }

            return new DataOperationResult(ErrorCodes.InvalidDocument, "Syntax error while trying to parse the remove command.");
        }

        /// <summary>
        /// Processes an update operation.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        /// <returns>The result of the operation.</returns>
        private DataOperationResult ProcessUpdateOperation(Document doc)
        {
            UpdateOperation op = new UpdateOperation(doc);
            if (op.UpdateFields.ContainsKey("id") || op.RemoveFields.Contains("id"))
            {
                return new DataOperationResult(ErrorCodes.InvalidDocument, "Cannot modify the ObjectId of a document after it is created.");
            }

            if (op.Valid)
            {
                lock (_chunks)
                {
                    var chunk = GetChunk(op.DocumentId);

                    if (chunk.ContainsKey(op.DocumentId))
                    {
                        Document toUpdate = chunk[op.DocumentId];
                        toUpdate.Merge(op.UpdateFields);
                        foreach (var field in op.RemoveFields)
                        {
                            toUpdate.RemoveField(field);
                        }

                        return new DataOperationResult(toUpdate);
                    }

                    return new DataOperationResult(ErrorCodes.InvalidId, "The specified document was not found.");
                }
            }

            return new DataOperationResult(ErrorCodes.InvalidDocument, "Syntax error while trying to parse the update command.");
        }
    }
}
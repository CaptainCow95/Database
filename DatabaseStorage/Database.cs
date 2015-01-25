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
        /// The database's data.
        /// </summary>
        private readonly SortedDictionary<ObjectId, Document> _data = new SortedDictionary<ObjectId, Document>();

        /// <summary>
        /// The system's id.
        /// </summary>
        private readonly Guid _systemId = Guid.NewGuid();

        /// <summary>
        /// The counter for generating new <see cref="ObjectId"/>s.
        /// </summary>
        private int _idCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        public Database()
        {
            Random rand = new Random();
            _idCounter = rand.Next();
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
                return new DataOperationResult("{\"success\":false,\"error\":\"Invalid document.\"}");
            }

            if (doc.Count != 1)
            {
                return new DataOperationResult("{\"success\":false,\"error\":\"Must have one and only one operation at a time.\"}");
            }

            var item = doc.First();
            switch (item.Key)
            {
                case "add":
                    if (doc.CheckForSubkeys())
                    {
                        return new DataOperationResult("{\"success\":false,\"error\":\"Subkeys are not allowed in the add operation.\"}");
                    }

                    return ProcessAddOperation(item.Value.Value as Document);

                case "update":
                    if (doc.CheckForSubkeys())
                    {
                        return new DataOperationResult("{\"success\":false,\"error\":\"Subkeys are not allowed in the update operation.\"}");
                    }

                    return ProcessUpdateOperation(item.Value.Value as Document);

                case "remove":
                    if (doc.CheckForSubkeys())
                    {
                        return new DataOperationResult("{\"success\":false,\"error\":\"Subkeys are not allowed in the remove operation.\"}");
                    }

                    return ProcessRemoveOperation(item.Value.Value as Document);

                case "query":
                    return ProcessQueryOperation(item.Value.Value as Document);

                default:
                    return new DataOperationResult("{\"success\":false,\"error\":\"Invalid operation specified\"}");
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
        /// Generates a new <see cref="ObjectId"/>.
        /// </summary>
        /// <returns>A new <see cref="ObjectId"/>.</returns>
        private ObjectId GenerateObjectId()
        {
            ObjectId id = new ObjectId(_systemId, _idCounter);
            Interlocked.Increment(ref _idCounter);
            return id;
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
                        catch (ArgumentException)
                        {
                            return new DataOperationResult("{\"success\":false,\"error\":\"Document contains an id field that is not an ObjectId.\"}");
                        }

                        lock (_data)
                        {
                            if (_data.ContainsKey(id))
                            {
                                return new DataOperationResult("{\"success\":false,\"error\":\"The database already contains a document with the specified id.\"}");
                            }
                        }
                    }
                    else
                    {
                        return new DataOperationResult("{\"success\":false,\"error\":\"Document contains an id field that is not an ObjectId.\"}");
                    }
                }
                else
                {
                    // Generate an id for the document.
                    var id = GenerateObjectId();
                    lock (_data)
                    {
                        while (_data.ContainsKey(id))
                        {
                            id = GenerateObjectId();
                        }

                        op.Document["id"] = new DocumentEntry("id", DocumentEntryType.String, id.ToString());
                        _data.Add(id, op.Document);
                    }
                }

                return new DataOperationResult("{\"success\":true,\"document\":" + op.Document.ToJson() + "}");
            }

            return new DataOperationResult("{\"success\":false,\"error\":\"Syntax error while trying to parse add command.\"}");
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
                try
                {
                    List<QueryItem> queryItems = BuildQueryItems(op.Fields);

                    List<Document> results = new List<Document>();
                    lock (_data)
                    {
                        foreach (var item in _data)
                        {
                            bool matches = queryItems.All(query => query.Match(item.Value));

                            if (matches)
                            {
                                results.Add(item.Value);
                            }
                        }
                    }

                    StringBuilder builder = new StringBuilder();
                    builder.Append("{\"success\":true,\"results\":{");

                    int count = 0;
                    bool first = true;
                    foreach (var result in results)
                    {
                        if (!first)
                        {
                            builder.Append(",");
                        }

                        builder.Append("\"" + count + "\":");
                        builder.Append(result.ToJson());
                        ++count;
                        first = false;
                    }

                    builder.Append(",\"count\":" + count);

                    builder.Append("}}");
                    return new DataOperationResult(builder.ToString());
                }
                catch (QueryException e)
                {
                    return new DataOperationResult("{\"success\":false,\"error\":\"" + e.Message.Replace("\"", "\\\"") + "\"");
                }
            }

            return new DataOperationResult("{\"success\":false,\"error\":\"Syntax error while trying to parse query command.\"}");
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
                lock (_data)
                {
                    _data.Remove(op.DocumentId);
                }
            }
            else
            {
                return new DataOperationResult("{\"success\":false,\"error\":\"Syntax error while trying to parse remove command.\"}");
            }

            return new DataOperationResult("{\"success\":true}");
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
                return new DataOperationResult("{\"success\":false,\"error\":\"Cannot modify the id value of a document after it is created.\"}");
            }

            if (op.Valid)
            {
                lock (_data)
                {
                    if (_data.ContainsKey(op.DocumentId))
                    {
                        Document toUpdate = _data[op.DocumentId];
                        toUpdate.Merge(op.UpdateFields);
                        foreach (var field in op.RemoveFields)
                        {
                            toUpdate.RemoveField(field);
                        }

                        return new DataOperationResult("{\"success\":true,\"document\":" + toUpdate.ToJson() + "}");
                    }

                    return new DataOperationResult("{\"success\":false,\"error\":\"Document not found.\"}");
                }
            }

            return new DataOperationResult("{\"success\":false,\"error\":\"Syntax error while trying to parse update command.\"}");
        }
    }
}
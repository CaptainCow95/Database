using System;
using System.Collections.Generic;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents an update operation.
    /// </summary>
    public class UpdateOperation
    {
        /// <summary>
        /// The document to be modified.
        /// </summary>
        private readonly ObjectId _documentId;

        /// <summary>
        /// The fields to be removed.
        /// </summary>
        private readonly List<string> _removeFields = new List<string>();

        /// <summary>
        /// The fields to be updated.
        /// </summary>
        private readonly Document _updateFields;

        /// <summary>
        /// A value indicating whether this operation is syntactically valid.
        /// </summary>
        private readonly bool _valid = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateOperation"/> class.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        public UpdateOperation(Document doc)
        {
            if (doc != null)
            {
                int foundFields = 0;
                if (doc.ContainsKey("documentId") && doc["documentId"].ValueType == DocumentEntryType.String)
                {
                    ++foundFields;
                    try
                    {
                        _documentId = new ObjectId(doc["documentId"].ValueAsString);
                    }
                    catch (Exception)
                    {
                        // invalid, stop processing and return since valid starts as false.
                        return;
                    }
                }
                else
                {
                    // The id of the document to update is required.
                    return;
                }

                if (doc.ContainsKey("updateFields") && doc["updateFields"].ValueType == DocumentEntryType.Document)
                {
                    ++foundFields;
                    _updateFields = doc["updateFields"].ValueAsDocument;
                }

                if (doc.ContainsKey("removeFields") && doc["removeFields"].ValueType == DocumentEntryType.Array)
                {
                    ++foundFields;
                    _removeFields = new List<string>();
                    foreach (var item in doc["removeFields"].ValueAsArray)
                    {
                        if (item.ValueType != DocumentEntryType.String)
                        {
                            return;
                        }

                        _removeFields.Add(item.ValueAsString);
                    }
                }

                if (foundFields == doc.Count)
                {
                    _valid = true;
                }
            }
        }

        /// <summary>
        /// Gets the document to be modified.
        /// </summary>
        public ObjectId DocumentId
        {
            get { return _documentId; }
        }

        /// <summary>
        /// Gets the fields that are to be removed.
        /// </summary>
        public List<string> RemoveFields
        {
            get { return _removeFields; }
        }

        /// <summary>
        /// Gets the fields that are to be updated.
        /// </summary>
        public Document UpdateFields
        {
            get { return _updateFields; }
        }

        /// <summary>
        /// Gets a value indicating whether this operation is syntactically valid.
        /// </summary>
        public bool Valid
        {
            get { return _valid; }
        }
    }
}
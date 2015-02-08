using Database.Common.Messages;
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
        /// The error message to send if the operation is not valid.
        /// </summary>
        private readonly DataOperationResult _errorMessage = null;

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
            if (doc == null)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The value under 'update' is not a valid document.");
                return;
            }

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
                    _errorMessage = new DataOperationResult(ErrorCodes.InvalidId, "The 'documentId' field is not a valid ObjectId.");
                    return;
                }
            }
            else if (doc.ContainsKey("documentId"))
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidId, "The 'documentId' field is not a string value.");
                return;
            }
            else
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidId, "The 'documentId' field is required for the update operation.");
                return;
            }

            if (doc.ContainsKey("updateFields") && doc["updateFields"].ValueType == DocumentEntryType.Document)
            {
                ++foundFields;
                _updateFields = doc["updateFields"].ValueAsDocument;
            }
            else if (doc.ContainsKey("updateFields"))
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The 'updateFields' field is present, but is not a valid document.");
                return;
            }

            if (doc.ContainsKey("removeFields") && doc["removeFields"].ValueType == DocumentEntryType.Array)
            {
                ++foundFields;
                _removeFields = new List<string>();
                foreach (var item in doc["removeFields"].ValueAsArray)
                {
                    if (item.ValueType != DocumentEntryType.String)
                    {
                        _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "Not all items in the 'removeFields' array is a string.");
                        return;
                    }

                    _removeFields.Add(item.ValueAsString);
                }
            }
            else if (doc.ContainsKey("removeFields"))
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The 'removeFields' field is present, but is not a valid array.");
                return;
            }

            if (foundFields != doc.Count)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The number of found fields in the 'update' document does not match the number of valid fields.");
            }

            if (_updateFields.ContainsKey("id") || _removeFields.Contains("id"))
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "Cannot modify the ObjectId of a document after it is created.");
                return;
            }

            _valid = true;
        }

        /// <summary>
        /// Gets the document to be modified.
        /// </summary>
        public ObjectId DocumentId
        {
            get { return _documentId; }
        }

        /// <summary>
        /// Gets the error message to send if the operation is not valid.
        /// </summary>
        public DataOperationResult ErrorMessage
        {
            get { return _errorMessage; }
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
using Database.Common.Messages;
using System;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents an add operation.
    /// </summary>
    public class AddOperation
    {
        /// <summary>
        /// The document to add.
        /// </summary>
        private readonly Document _document;

        /// <summary>
        /// The error message to send if the operation is not valid.
        /// </summary>
        private readonly DataOperationResult _errorMessage = null;

        /// <summary>
        /// The id of the document.
        /// </summary>
        private readonly ObjectId _id;

        /// <summary>
        /// A value indicating whether this operation is syntactically valid.
        /// </summary>
        private readonly bool _valid = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddOperation"/> class.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        public AddOperation(Document doc)
        {
            if (doc == null)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The value under 'add' is not a valid document.");
                return;
            }

            if (!doc.ContainsKey("document"))
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The 'document' field is required for the add operation.");
                return;
            }

            if (doc["document"].ValueType != DocumentEntryType.Document)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The 'document' field is not a valid document.");
                return;
            }

            if (doc.Count != 1)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The number of found fields in the 'add' document does not match the number of valid fields.");
                return;
            }

            _document = doc["document"].ValueAsDocument;

            if (!_document.ContainsKey("id"))
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidId, "Document does not contain an id field.");
                return;
            }

            if (_document["id"].ValueType != DocumentEntryType.String)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidId, "Document contains an id field that is not an ObjectId.");
                return;
            }

            try
            {
                _id = new ObjectId(_document["id"].ValueAsString);
            }
            catch (Exception)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidId, "Document contains an id field that is not an ObjectId.");
                return;
            }

            _valid = true;
        }

        /// <summary>
        /// Gets the document to add.
        /// </summary>
        public Document Document
        {
            get { return _document; }
        }

        /// <summary>
        /// Gets the error message to send if the operation is not valid.
        /// </summary>
        public DataOperationResult ErrorMessage
        {
            get { return _errorMessage; }
        }

        /// <summary>
        /// Gets the id of the document.
        /// </summary>
        public ObjectId Id
        {
            get { return _id; }
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
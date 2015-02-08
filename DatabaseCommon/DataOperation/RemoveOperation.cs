using Database.Common.Messages;
using System;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents a remove operation.
    /// </summary>
    public class RemoveOperation
    {
        /// <summary>
        /// The document to be removed.
        /// </summary>
        private readonly ObjectId _documentId;

        /// <summary>
        /// The error message to send if the operation is not valid.
        /// </summary>
        private readonly DataOperationResult _errorMessage = null;

        /// <summary>
        /// A value indicating whether this operation is syntactically valid.
        /// </summary>
        private readonly bool _valid = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveOperation"/> class.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        public RemoveOperation(Document doc)
        {
            if (doc == null)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The value under 'remove' is not a valid document.");
                return;
            }

            if (doc.ContainsKey("documentId") && doc["documentId"].ValueType == DocumentEntryType.String)
            {
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

            if (doc.Count != 1)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The number of found fields in the 'remove' document does not match the number of valid fields.");
            }

            _valid = true;
        }

        /// <summary>
        /// Gets the document to be removed.
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
        /// Gets a value indicating whether this operation is syntactically valid.
        /// </summary>
        public bool Valid
        {
            get { return _valid; }
        }
    }
}
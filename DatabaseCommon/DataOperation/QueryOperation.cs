using Database.Common.Messages;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents a query operation.
    /// </summary>
    public class QueryOperation
    {
        /// <summary>
        /// The error message to send if the operation is not valid.
        /// </summary>
        private readonly DataOperationResult _errorMessage = null;

        /// <summary>
        /// The fields to be queried.
        /// </summary>
        private readonly Document _fields;

        /// <summary>
        /// A value indicating whether this operation is syntactically valid.
        /// </summary>
        private readonly bool _valid = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryOperation"/> class.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        public QueryOperation(Document doc)
        {
            if (doc == null)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The value under \"query\" is not a valid document.");
                return;
            }

            int foundFields = 0;
            if (doc.ContainsKey("fields") && doc["fields"].ValueType == DocumentEntryType.Document)
            {
                ++foundFields;
                _fields = doc["fields"].ValueAsDocument;
            }
            else if (doc.ContainsKey("fields"))
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The \"fields\" field is present, but is not a valid document.");
                return;
            }
            else
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The \"fields\" field is required for the query operation.");
                return;
            }

            if (foundFields != doc.Count)
            {
                _errorMessage = new DataOperationResult(ErrorCodes.InvalidDocument, "The number of found fields in the \"query\" document does not match the number of valid fields.");
                return;
            }

            _valid = true;
        }

        /// <summary>
        /// Gets the error message to send if the operation is not valid.
        /// </summary>
        public DataOperationResult ErrorMessage
        {
            get { return _errorMessage; }
        }

        /// <summary>
        /// Gets the fields to be queried.
        /// </summary>
        public Document Fields
        {
            get { return _fields; }
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
namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents a query operation.
    /// </summary>
    public class QueryOperation
    {
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
            if (doc != null)
            {
                int foundFields = 0;
                if (doc.ContainsKey("fields") && doc["fields"].ValueType == DocumentEntryType.Document)
                {
                    ++foundFields;
                    _fields = doc["fields"].ValueAsDocument;
                }
                else
                {
                    // required field.
                    return;
                }

                if (foundFields == doc.Count)
                {
                    _valid = true;
                }
            }
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
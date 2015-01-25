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
        /// A value indicating whether this operation is syntactically valid.
        /// </summary>
        private readonly bool _valid = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddOperation"/> class.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        public AddOperation(Document doc)
        {
            if (doc != null && doc.Count == 1)
            {
                if (doc.ContainsKey("document") && doc["document"].ValueType == DocumentEntryType.Document)
                {
                    _document = doc["document"].ValueAsDocument;
                    _valid = true;
                }
            }
        }

        /// <summary>
        /// Gets the document to add.
        /// </summary>
        public Document Document
        {
            get { return _document; }
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
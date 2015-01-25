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
        /// A value indicating whether this operation is syntactically valid.
        /// </summary>
        private readonly bool _valid = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveOperation"/> class.
        /// </summary>
        /// <param name="doc">The document representing the operation.</param>
        public RemoveOperation(Document doc)
        {
            if (doc != null && doc.Count == 1)
            {
                if (doc.ContainsKey("documentId") && doc["documentId"].ValueType == DocumentEntryType.String)
                {
                    try
                    {
                        _documentId = new ObjectId(doc["documentId"].ValueAsString);
                        _valid = true;
                    }
                    catch (ArgumentException)
                    {
                        // invalid, stop processing and return since valid starts as false.
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the document to be removed.
        /// </summary>
        public ObjectId DocumentId
        {
            get { return _documentId; }
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
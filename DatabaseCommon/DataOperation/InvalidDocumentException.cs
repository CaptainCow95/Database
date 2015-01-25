using System;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Used when a document is invalid.
    /// </summary>
    internal class InvalidDocumentException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDocumentException"/> class.
        /// </summary>
        public InvalidDocumentException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDocumentException"/> class.
        /// </summary>
        /// <param name="reason">The reason for the exception.</param>
        public InvalidDocumentException(string reason)
            : base(reason)
        {
        }
    }
}
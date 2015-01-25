using System;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents an exception during query generation.
    /// </summary>
    public class QueryException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException"/> class.
        /// </summary>
        /// <param name="message">The message to be shown.</param>
        public QueryException(string message)
            : base(message)
        {
        }
    }
}
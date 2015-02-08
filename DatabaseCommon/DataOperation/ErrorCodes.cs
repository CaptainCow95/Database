namespace Database.Common.DataOperation
{
    /// <summary>
    /// A list of the error codes that can be sent back from a request.
    /// </summary>
    public enum ErrorCodes
    {
        /// <summary>
        /// The operation contained an invalid document.
        /// </summary>
        InvalidDocument,

        /// <summary>
        /// The operation requested does not allow sub-keys.
        /// </summary>
        SubkeysNotAllowed,

        /// <summary>
        /// The operation message failed to make it all the way to the database.
        /// </summary>
        FailedMessage,

        /// <summary>
        /// The operation contained an invalid id.
        /// </summary>
        InvalidId
    }
}
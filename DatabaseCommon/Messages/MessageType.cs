namespace Database.Common.Messages
{
    /// <summary>
    /// Represents the type of the message.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// A JoinAttempt message.
        /// </summary>
        JoinAttempt = 0,

        /// <summary>
        /// A JoinSuccess message.
        /// </summary>
        JoinSuccess = 1,

        /// <summary>
        /// A JoinFailure message.
        /// </summary>
        JoinFailure = 2,

        /// <summary>
        /// A Heartbeat message.
        /// </summary>
        Heartbeat = 3,
    }
}
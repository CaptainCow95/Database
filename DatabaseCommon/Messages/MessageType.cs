namespace Database.Common.Messages
{
    /// <summary>
    /// Represents the type of the message.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// A <see cref="JoinAttempt"/> message.
        /// </summary>
        JoinAttempt = 0,

        /// <summary>
        /// A <see cref="JoinSuccess"/> message.
        /// </summary>
        JoinSuccess = 1,

        /// <summary>
        /// A <see cref="JoinFailure"/> message.
        /// </summary>
        JoinFailure = 2,

        /// <summary>
        /// A <see cref="Heartbeat"/> message.
        /// </summary>
        Heartbeat = 3,

        /// <summary>
        /// A <see cref="VotingRequest"/> message.
        /// </summary>
        VotingRequest = 4,

        /// <summary>
        /// A <see cref="VotingResponse"/> message.
        /// </summary>
        VotingResponse = 5,

        /// <summary>
        /// A <see cref="LastPrimaryMessageIdRequest"/> message.
        /// </summary>
        LastPrimaryMessageIdRequest = 6,

        /// <summary>
        /// A <see cref="LastPrimaryMessageIdResponse"/> message.
        /// </summary>
        LastPrimaryMessageIdResponse = 7,

        /// <summary>
        /// A <see cref="PrimaryAnnouncement"/> message.
        /// </summary>
        PrimaryAnnouncement = 8,

        DataOperation,
        DataOperationResult
    }
}
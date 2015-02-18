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

        /// <summary>
        /// A <see cref="DataOperation"/> message.
        /// </summary>
        DataOperation = 9,

        /// <summary>
        /// A <see cref="DataOperationResult"/> message.
        /// </summary>
        DataOperationResult = 10,

        /// <summary>
        /// A <see cref="NodeList"/> message.
        /// </summary>
        NodeList = 11,

        /// <summary>
        /// A <see cref="ChunkListUpdate"/> message.
        /// </summary>
        ChunkListUpdate = 12,

        /// <summary>
        /// A <see cref="ChunkSplit"/> message.
        /// </summary>
        ChunkSplit = 13,

        /// <summary>
        /// A <see cref="ChunkMerge"/> message.
        /// </summary>
        ChunkMerge = 14,

        /// <summary>
        /// A <see cref="Acknowledgement"/> message.
        /// </summary>
        Acknowledgement = 15,

        /// <summary>
        /// A <see cref="DatabaseCreate"/> message.
        /// </summary>
        DatabaseCreate = 16,

        /// <summary>
        /// A <see cref="ChunkManagementRequest"/> message.
        /// </summary>
        ChunkManagementRequest = 17,

        /// <summary>
        /// A <see cref="ChunkManagementResponse"/> message.
        /// </summary>
        ChunkManagementResponse = 18,

        /// <summary>
        /// A <see cref="ChunkTransfer"/> message.
        /// </summary>
        ChunkTransfer = 19,

        /// <summary>
        /// A <see cref="ChunkTransferComplete"/> message.
        /// </summary>
        ChunkTransferComplete = 20,

        /// <summary>
        /// A <see cref="ChunkDataRequest"/> message.
        /// </summary>
        ChunkDataRequest = 21,

        /// <summary>
        /// A <see cref="ChunkDataResponse"/> message.
        /// </summary>
        ChunkDataResponse = 22,

        /// <summary>
        /// A <see cref="ChunkListRequest"/> message.
        /// </summary>
        ChunkListRequest = 23,

        /// <summary>
        /// A <see cref="ChunkListResponse"/> message.
        /// </summary>
        ChunkListResponse = 24,
    }
}
namespace Database.Common.Messages
{
    /// <summary>
    /// Represents a response to a <see cref="LastPrimaryMessageIdRequest"/> message.
    /// </summary>
    public class LastPrimaryMessageIdResponse : BaseMessageData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LastPrimaryMessageIdResponse"/> class.
        /// </summary>
        /// <param name="lastMessageId">The last message id received from the primary controller.</param>
        public LastPrimaryMessageIdResponse(uint lastMessageId)
        {
            LastMessageId = lastMessageId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LastPrimaryMessageIdResponse"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        public LastPrimaryMessageIdResponse(byte[] data, int index)
        {
            LastMessageId = ByteArrayHelper.ToUInt32(data, ref index);
        }

        /// <summary>
        /// Gets the last message id received from the primary controller.
        /// </summary>
        public uint LastMessageId { get; private set; }

        /// <inheritdoc />
        public override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(LastMessageId);
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.LastPrimaryMessageIdResponse;
        }
    }
}
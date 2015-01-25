namespace Database.Common.Messages
{
    /// <summary>
    /// Represents a response to a voting request.
    /// </summary>
    public class VotingResponse : BaseMessageData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VotingResponse"/> class.
        /// </summary>
        /// <param name="answer">The answer from the voting request.</param>
        public VotingResponse(bool answer)
        {
            Answer = answer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VotingResponse"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        public VotingResponse(byte[] data, int index)
        {
            Answer = ByteArrayHelper.ToBoolean(data, ref index);
        }

        /// <summary>
        /// Gets a value indicating whether the answer from the voting request is yes or no.
        /// </summary>
        public bool Answer { get; private set; }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(Answer);
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.VotingResponse;
        }
    }
}
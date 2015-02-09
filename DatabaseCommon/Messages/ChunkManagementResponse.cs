namespace Database.Common.Messages
{
    /// <summary>
    /// Sent in response to a <see cref="ChunkManagementRequest"/> message.
    /// </summary>
    public class ChunkManagementResponse : BaseMessageData
    {
        /// <summary>
        /// A value indicating whether the request was successful.
        /// </summary>
        private readonly bool _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkManagementResponse"/> class.
        /// </summary>
        /// <param name="result">A value indicating whether the request was successful.</param>
        public ChunkManagementResponse(bool result)
        {
            _result = result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkManagementResponse"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        public ChunkManagementResponse(byte[] data, int index)
        {
            _result = ByteArrayHelper.ToBoolean(data, ref index);
        }

        /// <summary>
        /// Gets a value indicating whether the request was successful.
        /// </summary>
        public bool Result
        {
            get { return _result; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(_result);
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.ChunkManagementResponse;
        }
    }
}
namespace Database.Common.Messages
{
    /// <summary>
    /// Sent when a chunk transfer has been completed.
    /// </summary>
    public class ChunkTransferComplete : BaseMessageData
    {
        /// <summary>
        /// A value indicating whether the value was successful.
        /// </summary>
        private readonly bool _success;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkTransferComplete"/> class.
        /// </summary>
        /// <param name="success">A value indicating whether the transfer was successful.</param>
        public ChunkTransferComplete(bool success)
        {
            _success = success;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkTransferComplete"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        internal ChunkTransferComplete(byte[] data, int index)
        {
            _success = ByteArrayHelper.ToBoolean(data, ref index);
        }

        /// <summary>
        /// Gets a value indicating whether the transfer was successful.
        /// </summary>
        public bool Success
        {
            get { return _success; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(_success);
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.ChunkTransferComplete;
        }
    }
}
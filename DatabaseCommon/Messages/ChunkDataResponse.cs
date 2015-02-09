using Database.Common.DataOperation;

namespace Database.Common.Messages
{
    /// <summary>
    /// Sent in response to a <see cref="ChunkDataRequest"/> message.
    /// </summary>
    public class ChunkDataResponse : BaseMessageData
    {
        /// <summary>
        /// The data that was in the chunk.
        /// </summary>
        private readonly Document _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkDataResponse"/> class.
        /// </summary>
        /// <param name="data">The data that was in the chunk.</param>
        public ChunkDataResponse(Document data)
        {
            _data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkDataResponse"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        public ChunkDataResponse(byte[] data, int index)
        {
            _data = new Document(ByteArrayHelper.ToString(data, ref index));
        }

        /// <summary>
        /// Gets the data that was in the chunk.
        /// </summary>
        public Document Data
        {
            get { return _data; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(_data.ToJson());
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.ChunkDataResponse;
        }
    }
}
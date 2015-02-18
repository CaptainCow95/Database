using Database.Common.DataOperation;

namespace Database.Common.Messages
{
    /// <summary>
    /// Represents two chunks that have just been merged.
    /// </summary>
    public class ChunkMerge : BaseMessageData
    {
        /// <summary>
        /// The chunk's end marker.
        /// </summary>
        private readonly ChunkMarker _end;

        /// <summary>
        /// The chunk's start marker.
        /// </summary>
        private readonly ChunkMarker _start;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkMerge"/> class.
        /// </summary>
        /// <param name="start">The chunk's start marker.</param>
        /// <param name="end">The chunk's end marker.</param>
        public ChunkMerge(ChunkMarker start, ChunkMarker end)
        {
            _start = start;
            _end = end;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkMerge"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        internal ChunkMerge(byte[] data, int index)
        {
            _start = ChunkMarker.ConvertFromString(ByteArrayHelper.ToString(data, ref index));
            _end = ChunkMarker.ConvertFromString(ByteArrayHelper.ToString(data, ref index));
        }

        /// <summary>
        /// Gets the chunk's end marker.
        /// </summary>
        public ChunkMarker End
        {
            get { return _end; }
        }

        /// <summary>
        /// Gets the chunk's start marker.
        /// </summary>
        public ChunkMarker Start
        {
            get { return _start; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.Combine(
                ByteArrayHelper.ToBytes(_start.ToString()),
                ByteArrayHelper.ToBytes(_end.ToString()));
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.ChunkMerge;
        }
    }
}
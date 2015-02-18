using Database.Common.DataOperation;

namespace Database.Common.Messages
{
    /// <summary>
    /// Represents a chunk that has just been split.
    /// </summary>
    public class ChunkSplit : BaseMessageData
    {
        /// <summary>
        /// The first chunk's end marker.
        /// </summary>
        private readonly ChunkMarker _end1;

        /// <summary>
        /// The second chunk's end marker.
        /// </summary>
        private readonly ChunkMarker _end2;

        /// <summary>
        /// The first chunk's start marker.
        /// </summary>
        private readonly ChunkMarker _start1;

        /// <summary>
        /// The second chunk's start marker.
        /// </summary>
        private readonly ChunkMarker _start2;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkSplit"/> class.
        /// </summary>
        /// <param name="start1">The first chunk's start marker.</param>
        /// <param name="end1">The first chunk's end marker.</param>
        /// <param name="start2">The second chunk's start marker.</param>
        /// <param name="end2">The second chunk's end marker.</param>
        public ChunkSplit(ChunkMarker start1, ChunkMarker end1, ChunkMarker start2, ChunkMarker end2)
        {
            _start1 = start1;
            _end1 = end1;
            _start2 = start2;
            _end2 = end2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkSplit"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        internal ChunkSplit(byte[] data, int index)
        {
            _start1 = ChunkMarker.ConvertFromString(ByteArrayHelper.ToString(data, ref index));
            _end1 = ChunkMarker.ConvertFromString(ByteArrayHelper.ToString(data, ref index));
            _start2 = ChunkMarker.ConvertFromString(ByteArrayHelper.ToString(data, ref index));
            _end2 = ChunkMarker.ConvertFromString(ByteArrayHelper.ToString(data, ref index));
        }

        /// <summary>
        /// Gets the first chunk's end marker.
        /// </summary>
        public ChunkMarker End1
        {
            get { return _end1; }
        }

        /// <summary>
        /// Gets the second chunk's end marker.
        /// </summary>
        public ChunkMarker End2
        {
            get { return _end2; }
        }

        /// <summary>
        /// Gets the first chunk's start marker.
        /// </summary>
        public ChunkMarker Start1
        {
            get { return _start1; }
        }

        /// <summary>
        /// Gets the second chunk's start marker.
        /// </summary>
        public ChunkMarker Start2
        {
            get { return _start2; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.Combine(
                ByteArrayHelper.ToBytes(_start1.ToString()),
                ByteArrayHelper.ToBytes(_end1.ToString()),
                ByteArrayHelper.ToBytes(_start2.ToString()),
                ByteArrayHelper.ToBytes(_end2.ToString()));
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.ChunkSplit;
        }
    }
}
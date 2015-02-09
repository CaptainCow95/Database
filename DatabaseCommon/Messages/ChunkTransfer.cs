using Database.Common.DataOperation;

namespace Database.Common.Messages
{
    /// <summary>
    /// Sent to begin a chunk transfer.
    /// </summary>
    public class ChunkTransfer : BaseMessageData
    {
        /// <summary>
        /// The end of the chunk.
        /// </summary>
        private readonly ChunkMarker _end;

        /// <summary>
        /// The node the chunk is on.
        /// </summary>
        private readonly NodeDefinition _node;

        /// <summary>
        /// The start of the chunk.
        /// </summary>
        private readonly ChunkMarker _start;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkTransfer"/> class.
        /// </summary>
        /// <param name="node">The node the chunk is on.</param>
        /// <param name="start">The start of the chunk.</param>
        /// <param name="end">The end of the chunk.</param>
        public ChunkTransfer(NodeDefinition node, ChunkMarker start, ChunkMarker end)
        {
            _node = node;
            _start = start;
            _end = end;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkTransfer"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        public ChunkTransfer(byte[] data, int index)
        {
            string node = ByteArrayHelper.ToString(data, ref index);
            _node = new NodeDefinition(node.Split(':')[0], int.Parse(node.Split(':')[1]));
            _start = ChunkMarker.ConvertFromString(ByteArrayHelper.ToString(data, ref index));
            _end = ChunkMarker.ConvertFromString(ByteArrayHelper.ToString(data, ref index));
        }

        /// <summary>
        /// Gets the end of the chunk.
        /// </summary>
        public ChunkMarker End
        {
            get { return _end; }
        }

        /// <summary>
        /// Gets the node the chunk is on.
        /// </summary>
        public NodeDefinition Node
        {
            get { return _node; }
        }

        /// <summary>
        /// Gets the start of the chunk.
        /// </summary>
        public ChunkMarker Start
        {
            get { return _start; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.Combine(
                ByteArrayHelper.ToBytes(_node.ConnectionName),
                ByteArrayHelper.ToBytes(_start.ToString()),
                ByteArrayHelper.ToBytes(_end.ToString()));
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.ChunkTransfer;
        }
    }
}
namespace Database.Common.DataOperation
{
    /// <summary>
    /// A class representing the different defining parts of a database chunk.
    /// </summary>
    public class ChunkDefinition
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
        /// Initializes a new instance of the <see cref="ChunkDefinition"/> class.
        /// </summary>
        /// <param name="start">The start of the chunk.</param>
        /// <param name="end">The end of the chunk.</param>
        /// <param name="node">The node the chunk is on.</param>
        public ChunkDefinition(ChunkMarker start, ChunkMarker end, NodeDefinition node)
        {
            _start = start;
            _end = end;
            _node = node;
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
    }
}
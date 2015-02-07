namespace Database.Common.DataOperation
{
    public class ChunkDefinition
    {
        private readonly ChunkMarker _end;
        private readonly NodeDefinition _node;
        private readonly ChunkMarker _start;

        public ChunkDefinition(ChunkMarker start, ChunkMarker end, NodeDefinition node)
        {
            _start = start;
            _end = end;
            _node = node;
        }

        public ChunkMarker End
        {
            get { return _end; }
        }

        public NodeDefinition Node
        {
            get { return _node; }
        }

        public ChunkMarker Start
        {
            get { return _start; }
        }
    }
}
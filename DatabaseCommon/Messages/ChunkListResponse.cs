using Database.Common.DataOperation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Database.Common.Messages
{
    /// <summary>
    /// A message that is a response to a <see cref="ChunkListRequest"/> message.
    /// </summary>
    public class ChunkListResponse : BaseMessageData
    {
        /// <summary>
        /// The list of chunks.
        /// </summary>
        private readonly List<ChunkDefinition> _chunks;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkListResponse"/> class.
        /// </summary>
        /// <param name="chunks">The list of chunks.</param>
        public ChunkListResponse(List<ChunkDefinition> chunks)
        {
            _chunks = chunks;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkListResponse"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        internal ChunkListResponse(byte[] data, int index)
        {
            _chunks = new List<ChunkDefinition>();
            string s = ByteArrayHelper.ToString(data, ref index);
            string[] defs = s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in defs)
            {
                string[] def = item.Split(',');
                NodeDefinition nodeDef = new NodeDefinition(def[2].Split(':')[0], int.Parse(def[2].Split(':')[1]));
                _chunks.Add(new ChunkDefinition(ChunkMarker.ConvertFromString(def[0]), ChunkMarker.ConvertFromString(def[1]), nodeDef));
            }
        }

        /// <summary>
        /// Gets the list of chunks.
        /// </summary>
        public List<ChunkDefinition> Chunks
        {
            get { return _chunks; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var item in _chunks)
            {
                builder.AppendFormat("{0},{1},{2};", item.Start, item.End, item.Node.ConnectionName);
            }

            return ByteArrayHelper.ToBytes(builder.ToString());
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.ChunkListResponse;
        }
    }
}
using Database.Common.DataOperation;
using System.Collections.Generic;

namespace Database.Common.Messages
{
    /// <summary>
    /// Represents a <see cref="ChunkListUpdate"/> message.
    /// </summary>
    public class ChunkListUpdate : BaseMessageData
    {
        /// <summary>
        /// A document-based view of the chunk list.
        /// </summary>
        private readonly Document _chunkList;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkListUpdate"/> class.
        /// </summary>
        /// <param name="chunkList">The chunk list to send.</param>
        public ChunkListUpdate(List<ChunkDefinition> chunkList)
        {
            _chunkList = new Document();
            for (int i = 0; i < chunkList.Count; ++i)
            {
                List<DocumentEntry> arrayEntry = new List<DocumentEntry>()
                {
                    new DocumentEntry(string.Empty, DocumentEntryType.String, chunkList[i].Start.ToString()),
                    new DocumentEntry(string.Empty, DocumentEntryType.String, chunkList[i].End.ToString()),
                    new DocumentEntry(string.Empty, DocumentEntryType.String, chunkList[i].Node.ConnectionName)
                };

                _chunkList[i.ToString()] = new DocumentEntry(i.ToString(), DocumentEntryType.Array, arrayEntry);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkListUpdate"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        public ChunkListUpdate(byte[] data, int index)
        {
            _chunkList = new Document(ByteArrayHelper.ToString(data, ref index));
        }

        /// <summary>
        /// Gets the list of chunks.
        /// </summary>
        public List<ChunkDefinition> ChunkList
        {
            get { return GetList(); }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(_chunkList.ToJson());
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.ChunkListUpdate;
        }

        /// <summary>
        /// Gets a list of the chunks from the internal document.
        /// </summary>
        /// <returns>A list of the chunks from the internal document.</returns>
        private List<ChunkDefinition> GetList()
        {
            List<ChunkDefinition> returnValue = new List<ChunkDefinition>();
            for (int i = 0; i < _chunkList.Count; ++i)
            {
                var array = _chunkList[i.ToString()].ValueAsArray;
                var start = ChunkMarker.ConvertFromString(array[0].ValueAsString);
                var end = ChunkMarker.ConvertFromString(array[1].ValueAsString);
                var node = new NodeDefinition(array[2].ValueAsString.Split(':')[0], int.Parse(array[2].ValueAsString.Split(':')[1]));
                returnValue.Add(new ChunkDefinition(start, end, node));
            }

            return returnValue;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Database.Common.Messages
{
    /// <summary>
    /// Represents a message that contains all active storage nodes.
    /// </summary>
    public class StorageNodeConnection : BaseMessageData
    {
        /// <summary>
        /// A list of the storage nodes.
        /// </summary>
        private readonly List<string> _storageNodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageNodeConnection"/> class.
        /// </summary>
        /// <param name="storageNodes">A list of the storage nodes.</param>
        public StorageNodeConnection(List<string> storageNodes)
        {
            _storageNodes = storageNodes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageNodeConnection"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        public StorageNodeConnection(byte[] data, int index)
        {
            _storageNodes = ByteArrayHelper.ToString(data, ref index).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>
        /// Gets a list of the storage nodes.
        /// </summary>
        public List<string> StorageNodes
        {
            get { return _storageNodes; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            bool first = true;
            StringBuilder builder = new StringBuilder();
            foreach (var item in _storageNodes)
            {
                if (!first)
                {
                    builder.Append(",");
                }

                builder.Append(item);
                first = false;
            }

            return ByteArrayHelper.ToBytes(builder.ToString());
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.StorageNodeConnection;
        }
    }
}
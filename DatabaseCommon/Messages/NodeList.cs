using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Database.Common.Messages
{
    /// <summary>
    /// Represents a message that contains all active storage nodes.
    /// </summary>
    public class NodeList : BaseMessageData
    {
        /// <summary>
        /// A list of the nodes.
        /// </summary>
        private readonly List<string> _nodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeList"/> class.
        /// </summary>
        /// <param name="nodes">A list of the nodes.</param>
        public NodeList(List<string> nodes)
        {
            _nodes = nodes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeList"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        internal NodeList(byte[] data, int index)
        {
            _nodes = ByteArrayHelper.ToString(data, ref index).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>
        /// Gets a list of the nodes.
        /// </summary>
        public List<string> Nodes
        {
            get { return _nodes; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            bool first = true;
            StringBuilder builder = new StringBuilder();
            foreach (var item in _nodes)
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
            return (int)MessageType.NodeList;
        }
    }
}
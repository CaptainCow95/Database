using Database.Common.DataOperation;

namespace Database.Common.Messages
{
    /// <summary>
    /// A message to indicate that the join was successful.
    /// </summary>
    public class JoinSuccess : BaseMessageData
    {
        /// <summary>
        /// The data sent by the node being joined.
        /// </summary>
        private readonly Document _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinSuccess"/> class.
        /// </summary>
        /// <param name="data">The data sent by the node being joined.</param>
        public JoinSuccess(Document data)
        {
            _data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinSuccess"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        internal JoinSuccess(byte[] data, int index)
        {
            _data = new Document(ByteArrayHelper.ToString(data, ref index));
        }

        /// <summary>
        /// Gets the data sent by the node being joined.
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
            return (int)MessageType.JoinSuccess;
        }
    }
}
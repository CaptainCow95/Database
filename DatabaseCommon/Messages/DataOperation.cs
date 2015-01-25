namespace Database.Common.Messages
{
    /// <summary>
    /// Represents a request to do an operation.
    /// </summary>
    public class DataOperation : BaseMessageData
    {
        /// <summary>
        /// The JSON representing the operation.
        /// </summary>
        private readonly string _json;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOperation"/> class.
        /// </summary>
        /// <param name="json">The JSON representing the operation.</param>
        public DataOperation(string json)
        {
            _json = json;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOperation"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        internal DataOperation(byte[] data, int index)
        {
            _json = ByteArrayHelper.ToString(data, ref index);
        }

        /// <summary>
        /// Gets the JSON representing the operation.
        /// </summary>
        public string Json
        {
            get { return _json; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(_json);
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.DataOperation;
        }
    }
}
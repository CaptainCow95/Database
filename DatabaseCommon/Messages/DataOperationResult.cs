namespace Database.Common.Messages
{
    /// <summary>
    /// Represents the result of a <see cref="DataOperation"/> message.
    /// </summary>
    public class DataOperationResult : BaseMessageData
    {
        /// <summary>
        /// The result of the operation.
        /// </summary>
        private readonly string _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOperationResult"/> class.
        /// </summary>
        /// <param name="result">The result of the operation.</param>
        public DataOperationResult(string result)
        {
            _result = result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOperationResult"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        internal DataOperationResult(byte[] data, int index)
        {
            _result = ByteArrayHelper.ToString(data, ref index);
        }

        /// <summary>
        /// Gets the result of the operation.
        /// </summary>
        public string Result
        {
            get { return _result; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.Combine(ByteArrayHelper.ToBytes(_result));
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.DataOperationResult;
        }
    }
}
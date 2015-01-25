namespace Database.Common.Messages
{
    /// <summary>
    /// A message to indicate that the join was failed.
    /// </summary>
    public class JoinFailure : BaseMessageData
    {
        /// <summary>
        /// The reason the join failed.
        /// </summary>
        private readonly string _reason;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinFailure"/> class.
        /// </summary>
        /// <param name="reason">The reason for the failure.</param>
        public JoinFailure(string reason)
        {
            _reason = reason;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinFailure"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        public JoinFailure(byte[] data, int index)
        {
            _reason = ByteArrayHelper.ToString(data, ref index);
        }

        /// <summary>
        /// Gets the reason the join failed.
        /// </summary>
        public string Reason
        {
            get { return _reason; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(_reason);
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.JoinFailure;
        }
    }
}
namespace Database.Common.Messages
{
    /// <summary>
    /// A message to indicate that the join was successful.
    /// </summary>
    public class JoinSuccess : BaseMessageData
    {
        /// <summary>
        /// A value indicating whether the node is the primary controller.
        /// </summary>
        private bool _primaryController;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinSuccess"/> class.
        /// </summary>
        /// <param name="primaryController">Whether the node is the primary controller.</param>
        public JoinSuccess(bool primaryController)
        {
            _primaryController = primaryController;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinSuccess"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        public JoinSuccess(byte[] data, int index)
        {
            _primaryController = ByteArrayHelper.ToBoolean(data, ref index);
        }

        /// <summary>
        /// Gets a value indicating whether the node is the primary controller.
        /// </summary>
        public bool PrimaryController
        {
            get { return _primaryController; }
        }

        /// <inheritdoc />
        public override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(_primaryController);
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.JoinSuccess;
        }
    }
}
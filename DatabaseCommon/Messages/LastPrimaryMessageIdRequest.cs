namespace Database.Common.Messages
{
    /// <summary>
    /// Represents a request to get the last message id received from the primary controller.
    /// </summary>
    public class LastPrimaryMessageIdRequest : BaseMessageData
    {
        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.LastPrimaryMessageIdRequest;
        }
    }
}
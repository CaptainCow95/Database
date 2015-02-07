namespace Database.Common.Messages
{
    /// <summary>
    /// Represents a simple acknowledgement message.
    /// </summary>
    public class Acknowledgement : BaseMessageData
    {
        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.Acknowledgement;
        }
    }
}
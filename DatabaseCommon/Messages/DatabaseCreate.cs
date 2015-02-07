namespace Database.Common.Messages
{
    /// <summary>
    /// Sent when a database is to be created on a node.
    /// </summary>
    public class DatabaseCreate : BaseMessageData
    {
        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.DatabaseCreate;
        }
    }
}
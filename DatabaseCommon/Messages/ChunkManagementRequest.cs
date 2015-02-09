namespace Database.Common.Messages
{
    /// <summary>
    /// Used to signal a request to split or merge some chunks.
    /// </summary>
    public class ChunkManagementRequest : BaseMessageData
    {
        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.ChunkManagementRequest;
        }
    }
}
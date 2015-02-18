namespace Database.Common.Messages
{
    /// <summary>
    /// A request for the list of chunks a storage node contains.
    /// </summary>
    public class ChunkListRequest : BaseMessageData
    {
        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.ChunkListRequest;
        }
    }
}
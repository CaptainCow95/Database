namespace Database.Common.Messages
{
    /// <summary>
    /// Represents a request to begin voting.
    /// </summary>
    public class VotingRequest : BaseMessageData
    {
        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.VotingRequest;
        }
    }
}
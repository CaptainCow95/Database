namespace Database.Common.Messages
{
    /// <summary>
    /// Represents an announcement that a new primary controller has been selected.
    /// </summary>
    public class PrimaryAnnouncement : BaseMessageData
    {
        /// <inheritdoc />
        public override byte[] EncodeInternal()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.PrimaryAnnouncement;
        }
    }
}
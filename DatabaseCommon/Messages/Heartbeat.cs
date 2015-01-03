namespace Database.Common.Messages
{
    /// <summary>
    /// A message that acts like a ping.
    /// </summary>
    public class Heartbeat : BaseMessageData
    {
        /// <inheritdoc />
        public override byte[] EncodeInternal()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.Heartbeat;
        }
    }
}
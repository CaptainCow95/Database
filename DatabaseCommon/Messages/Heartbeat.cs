namespace Database.Common.Messages
{
    public class Heartbeat : BaseMessageData
    {
        public override byte[] EncodeInternal()
        {
            return new byte[0];
        }

        protected override int GetMessageTypeId()
        {
            return (int)MessageType.Heartbeat;
        }
    }
}
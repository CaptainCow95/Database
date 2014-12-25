namespace Database.Common.Messages
{
    public class JoinFailure : BaseMessageData
    {
        private string _reason;

        public JoinFailure(string reason)
        {
            _reason = reason;
        }

        public JoinFailure(byte[] data, int index)
        {
            _reason = ByteArrayHelper.ToString(data, ref index);
        }

        public string Reason { get { return _reason; } }

        public override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(_reason);
        }

        protected override int GetMessageTypeId()
        {
            return (int)MessageType.JoinFailure;
        }
    }
}
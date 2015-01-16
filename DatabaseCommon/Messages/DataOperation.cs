namespace Database.Common.Messages
{
    public class DataOperation : BaseMessageData
    {
        private string _operation;

        public DataOperation(string operation)
        {
            _operation = operation;
        }

        public DataOperation(byte[] data, int index)
        {
            _operation = ByteArrayHelper.ToString(data, ref index);
        }

        public string Operation { get { return _operation; } }

        public override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(_operation);
        }

        protected override int GetMessageTypeId()
        {
            return (int)MessageType.DataOperation;
        }
    }
}
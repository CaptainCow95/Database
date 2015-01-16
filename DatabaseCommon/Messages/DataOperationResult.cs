namespace Database.Common.Messages
{
    public class DataOperationResult : BaseMessageData
    {
        private string _result;

        public DataOperationResult(string result)
        {
            _result = result;
        }

        public DataOperationResult(byte[] data, int index)
        {
            _result = ByteArrayHelper.ToString(data, ref index);
        }

        public string Result { get { return _result; } }

        public override byte[] EncodeInternal()
        {
            return ByteArrayHelper.Combine(ByteArrayHelper.ToBytes(_result));
        }

        protected override int GetMessageTypeId()
        {
            return (int)MessageType.DataOperationResult;
        }
    }
}
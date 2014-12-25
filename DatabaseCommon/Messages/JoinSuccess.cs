namespace Database.Common.Messages
{
    public class JoinSuccess : BaseMessageData
    {
        private bool _primaryController;

        public JoinSuccess(bool primaryController)
        {
            _primaryController = primaryController;
        }

        public JoinSuccess(byte[] data, int index)
        {
            _primaryController = ByteArrayHelper.ToBoolean(data, ref index);
        }

        public bool PrimaryController { get { return _primaryController; } }

        public override byte[] EncodeInternal()
        {
            return ByteArrayHelper.ToBytes(_primaryController);
        }

        protected override int GetMessageTypeId()
        {
            return (int)MessageType.JoinSuccess;
        }
    }
}
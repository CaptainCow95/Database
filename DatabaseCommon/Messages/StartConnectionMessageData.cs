using System;
using System.Text;

namespace Database.Common.Messages
{
    public class StartConnectionMessageData : BaseMessageData
    {
        private readonly bool _connectedToPrimaryMaster;
        private readonly string _name;
        private readonly NodeType _type;

        public StartConnectionMessageData(NodeType type, string name, bool connectedToPrimaryMaster)
        {
            _type = type;
            _name = name;
            _connectedToPrimaryMaster = connectedToPrimaryMaster;
        }

        internal StartConnectionMessageData(byte[] data)
        {
            int index = 0;
            _type = BitConverter.ToBoolean(data, index) == true ? NodeType.Master : NodeType.Client;
            index += 1;
            _connectedToPrimaryMaster = BitConverter.ToBoolean(data, index);
            index += 1;
            _name = BitConverter.ToString(data, index);
        }

        public override byte[] EncodeInternal()
        {
            return ByteArrayHelper.Combine(BitConverter.GetBytes(_type == NodeType.Master),
                BitConverter.GetBytes(_connectedToPrimaryMaster), Encoding.Default.GetBytes(_name));
        }

        protected override int GetMessageTypeId()
        {
            return (int)MessageType.StartConnectionMessage;
        }
    }
}
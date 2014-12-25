using System;

namespace Database.Common.Messages
{
    public class JoinAttempt : BaseMessageData
    {
        private string _name;
        private int _port;
        private string _settings;
        private NodeType _type;

        public JoinAttempt(NodeType type, string name, int port, string settings)
        {
            _type = type;
            _name = name;
            _port = port;
            _settings = settings;
        }

        internal JoinAttempt(byte[] data, int index)
        {
            _type = (NodeType)Enum.ToObject(typeof(NodeType), ByteArrayHelper.ToInt32(data, ref index));
            _name = ByteArrayHelper.ToString(data, ref index);
            _port = ByteArrayHelper.ToInt32(data, ref index);
            _settings = ByteArrayHelper.ToString(data, ref index);
        }

        public string Name { get { return _name; } }

        public int Port { get { return _port; } }

        public string Settings { get { return _settings; } }

        public NodeType Type { get { return _type; } }

        public override byte[] EncodeInternal()
        {
            byte[] typeBytes = ByteArrayHelper.ToBytes((int)_type);
            byte[] nameBytes = ByteArrayHelper.ToBytes(_name);
            byte[] portBytes = ByteArrayHelper.ToBytes(_port);
            byte[] settingsBytes = ByteArrayHelper.ToBytes(_settings);

            return ByteArrayHelper.Combine(typeBytes, nameBytes, portBytes, settingsBytes);
        }

        protected override int GetMessageTypeId()
        {
            return (int)MessageType.JoinAttempt;
        }
    }
}
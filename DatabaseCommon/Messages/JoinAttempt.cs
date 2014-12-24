using System;
using System.Text;

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

        internal JoinAttempt(byte[] data)
        {
            int index = 0;
            int nodeType = BitConverter.ToInt32(data, index);
            index += 4;
            _type = (NodeType)Enum.ToObject(typeof(NodeType), nodeType);
            int nodeNameLength = BitConverter.ToInt32(data, index);
            index += 4;
            _name = Encoding.UTF8.GetString(data, index, nodeNameLength);
            index += nodeNameLength;
            _port = BitConverter.ToInt32(data, index);
            index += 4;
            int settingsLength = BitConverter.ToInt32(data, index);
            index += 4;
            _settings = Encoding.UTF8.GetString(data, index, settingsLength);
            index += settingsLength;
        }

        public string Name { get { return _name; } }

        public int Port { get { return _port; } }

        public string Settings { get { return _settings; } }

        public NodeType Type { get { return _type; } }

        public override byte[] EncodeInternal()
        {
            byte[] typeBytes = BitConverter.GetBytes((int)_type);
            byte[] nameBytes = Encoding.UTF8.GetBytes(_name);
            byte[] nameLengthBytes = BitConverter.GetBytes(nameBytes.Length);
            byte[] portBytes = BitConverter.GetBytes(_port);
            byte[] settingsBytes = Encoding.UTF8.GetBytes(_settings);
            byte[] settingsLengthBytes = BitConverter.GetBytes(settingsBytes.Length);

            return ByteArrayHelper.Combine(typeBytes, nameLengthBytes, nameBytes, portBytes, settingsLengthBytes, settingsBytes);
        }

        protected override int GetMessageTypeId()
        {
            return (int)MessageType.JoinAttempt;
        }
    }
}
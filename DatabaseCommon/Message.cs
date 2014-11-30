using System;
using System.Linq;
using System.Threading;

namespace Database.Common
{
    public class Message
    {
        private static uint _nextId = 0;
        private static object _nextIdLockObject = new object();
        private string _address;
        private byte[] _data;
        private uint _id;
        private uint _inResponseTo;
        private Message _response;
        private bool _sendWithoutConfirmation = false;
        private MessageStatus _status;
        private bool _waitingForResponse;

        public Message(string address, byte[] data, bool waitingForResponse)
        {
            _address = address;
            _data = data;
            _status = MessageStatus.Created;
            _id = GetNextId();
            _waitingForResponse = waitingForResponse;
        }

        public Message(Message responseTo, byte[] data, bool waitingForResponse)
        {
            _address = responseTo.Address;
            _inResponseTo = responseTo._id;
            _data = data;
            _status = MessageStatus.Created;
            _id = GetNextId();
            _waitingForResponse = waitingForResponse;
        }

        internal Message(string address, byte[] data)
        {
            _address = address;
            DecodeMessage(data);
            _status = MessageStatus.Received;
        }

        public string Address
        {
            get { return _address; }
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public Message Response
        {
            get { return _response; }
            internal set { _response = value; }
        }

        public MessageStatus Status
        {
            get { return _status; }
            internal set { _status = value; }
        }

        public bool Success
        {
            get { return _status == MessageStatus.Sent || _status == MessageStatus.ResponseReceived; }
        }

        public bool WaitingForResponse
        {
            get { return _waitingForResponse; }
        }

        internal uint ID
        {
            get { return _id; }
        }

        internal uint InResponseTo
        {
            get { return _inResponseTo; }
        }

        internal bool SendWithoutConfirmation
        {
            get { return _sendWithoutConfirmation; }
            set { _sendWithoutConfirmation = value; }
        }

        public void BlockUntilDone()
        {
            while (_status != MessageStatus.Sending && _status != MessageStatus.WaitingForResponse)
            {
                Thread.Sleep(1);
            }
        }

        internal byte[] EncodeMessage()
        {
            byte[] idBytes = BitConverter.GetBytes(_id);
            byte[] inResponseToBytes = BitConverter.GetBytes(_inResponseTo);
            byte[] waitingForResponseBytes = BitConverter.GetBytes(_waitingForResponse);

            byte[] message =
                new byte[idBytes.Length + inResponseToBytes.Length + waitingForResponseBytes.Length + _data.Length];
            int index = 0;
            for (int i = 0; i < idBytes.Length; ++i, ++index)
            {
                message[index] = idBytes[i];
            }

            for (int i = 0; i < inResponseToBytes.Length; ++i, ++index)
            {
                message[index] = inResponseToBytes[i];
            }

            for (int i = 0; i < waitingForResponseBytes.Length; ++i, ++index)
            {
                message[index] = waitingForResponseBytes[i];
            }

            for (int i = 0; i < _data.Length; ++i, ++index)
            {
                message[index] = _data[i];
            }

            return message;
        }

        private void DecodeMessage(byte[] data)
        {
            int index = 0;
            _id = BitConverter.ToUInt32(data, index);
            index += 4;
            _inResponseTo = BitConverter.ToUInt32(data, index);
            index += 4;
            _waitingForResponse = BitConverter.ToBoolean(data, index);
            index += 1;
            _data = data.Skip(index).ToArray();
        }

        private uint GetNextId()
        {
            lock (_nextIdLockObject)
            {
                ++_nextId;
                if (_nextId == 0)
                {
                    ++_nextId;
                }

                return _nextId;
            }
        }
    }
}
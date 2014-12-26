using Database.Common.Messages;
using System.Threading;

namespace Database.Common
{
    /// <summary>
    /// Represents a network message.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// The object to lock on when giving out the next message's id.
        /// </summary>
        private static readonly object NextIdLockObject = new object();

        /// <summary>
        /// The id to use for the next message.
        /// </summary>
        private static uint _nextId = 0;

        /// <summary>
        /// The address where the message came from or will be sent to.
        /// </summary>
        private NodeDefinition _address;

        private BaseMessageData _data;

        /// <summary>
        /// The id of the message.
        /// </summary>
        private uint _id;

        /// <summary>
        /// The message id this is in response to, otherwise 0.
        /// </summary>
        private uint _inResponseTo;

        /// <summary>
        /// The response to this message.
        /// </summary>
        private Message _response;

        /// <summary>
        /// A value indicating whether the message should be sent, even if the connection has yet to be confirmed.
        /// </summary>
        private bool _sendWithoutConfirmation = false;

        /// <summary>
        /// The current status of the message.
        /// </summary>
        private MessageStatus _status;

        /// <summary>
        /// A value indicating whether this message is waiting for a response.
        /// </summary>
        private bool _waitingForResponse;

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="address">The address the message is to be sent to.</param>
        /// <param name="data">The data to be sent in the message.</param>
        /// <param name="waitingForResponse">Whether the message is waiting for a response.</param>
        public Message(NodeDefinition address, BaseMessageData data, bool waitingForResponse)
        {
            _address = address;
            _data = data;
            _status = MessageStatus.Created;
            _id = GetNextId();
            _waitingForResponse = waitingForResponse;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="responseTo">The message this is in response to.</param>
        /// <param name="data">The data to be sent in the message.</param>
        /// <param name="waitingForResponse">Whether the message is waiting for a response.</param>
        public Message(Message responseTo, BaseMessageData data, bool waitingForResponse)
        {
            _address = responseTo.Address;
            _inResponseTo = responseTo._id;
            _data = data;
            _status = MessageStatus.Created;
            _id = GetNextId();
            _waitingForResponse = waitingForResponse;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="address">The address this message is from.</param>
        /// <param name="data">The message data to decode.</param>
        internal Message(NodeDefinition address, byte[] data)
        {
            _address = address;
            DecodeMessage(data);
            _status = MessageStatus.Received;
        }

        /// <summary>
        /// Gets or sets the address where the message came from or will be sent to.
        /// </summary>
        public NodeDefinition Address
        {
            get { return _address; }
            set { _address = value; }
        }

        public BaseMessageData Data
        {
            get { return _data; }
        }

        /// <summary>
        /// Gets the response to this message.
        /// </summary>
        public Message Response
        {
            get { return _response; }
            internal set { _response = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the message should be sent, even if the connection has yet to be confirmed.
        /// </summary>
        public bool SendWithoutConfirmation
        {
            get { return _sendWithoutConfirmation; }
            set { _sendWithoutConfirmation = value; }
        }

        /// <summary>
        /// Gets the current status of the message.
        /// </summary>
        public MessageStatus Status
        {
            get { return _status; }
            internal set { _status = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the message was sent successfully if it is not waiting for a response, otherwise it indicates whether the response has been successfully received.
        /// </summary>
        public bool Success
        {
            get { return _status == MessageStatus.Sent || _status == MessageStatus.ResponseReceived; }
        }

        /// <summary>
        /// Gets a value indicating whether the message is waiting for a response.
        /// </summary>
        public bool WaitingForResponse
        {
            get { return _waitingForResponse; }
        }

        /// <summary>
        /// Gets the id of the message.
        /// </summary>
        internal uint ID
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the id of the message this is in response to, 0 if it isn't in response to any message.
        /// </summary>
        internal uint InResponseTo
        {
            get { return _inResponseTo; }
        }

        /// <summary>
        /// Blocks until an error occurs, the message is sent successfully if it isn't waiting for a response, or until a response is received if it is waiting for a response.
        /// </summary>
        public void BlockUntilDone()
        {
            while (_status == MessageStatus.Sending || _status == MessageStatus.WaitingForResponse)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Encodes the message for sending.
        /// </summary>
        /// <returns>The encoded message.</returns>
        internal byte[] EncodeMessage()
        {
            byte[] idBytes = ByteArrayHelper.ToBytes(_id);
            byte[] inResponseToBytes = ByteArrayHelper.ToBytes(_inResponseTo);
            byte[] waitingForResponseBytes = ByteArrayHelper.ToBytes(_waitingForResponse);
            byte[] rawData = _data.Encode();
            byte[] lengthBytes =
                ByteArrayHelper.ToBytes(idBytes.Length + inResponseToBytes.Length + waitingForResponseBytes.Length +
                                        rawData.Length);

            return ByteArrayHelper.Combine(lengthBytes, idBytes, inResponseToBytes, waitingForResponseBytes, rawData);
        }

        /// <summary>
        /// Decodes the received message.
        /// </summary>
        /// <param name="data">The data to be decoded.</param>
        private void DecodeMessage(byte[] data)
        {
            int index = 0;
            _id = ByteArrayHelper.ToUInt32(data, ref index);
            _inResponseTo = ByteArrayHelper.ToUInt32(data, ref index);
            _waitingForResponse = ByteArrayHelper.ToBoolean(data, ref index);
            _data = BaseMessageData.Decode(data, index);
        }

        /// <summary>
        /// Gets the next message id.
        /// </summary>
        /// <returns>The next message id.</returns>
        private uint GetNextId()
        {
            lock (NextIdLockObject)
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
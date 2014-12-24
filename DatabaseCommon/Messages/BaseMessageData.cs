using System;
using System.Linq;

namespace Database.Common.Messages
{
    public abstract class BaseMessageData
    {
        public static BaseMessageData Decode(byte[] data)
        {
            int messageTypeId = BitConverter.ToInt32(data, 0);
            MessageType messageTypeIdConverted = (MessageType)Enum.ToObject(typeof(MessageType), messageTypeId);
            switch (messageTypeIdConverted)
            {
                case MessageType.JoinAttempt:
                    return new JoinAttempt(data.Skip(4).ToArray());

                default:
                    throw new Exception("Message type id not found: " + messageTypeId);
            }
        }

        public byte[] Encode()
        {
            return ByteArrayHelper.Combine(BitConverter.GetBytes(GetMessageTypeId()), EncodeInternal());
        }

        public abstract byte[] EncodeInternal();

        protected abstract int GetMessageTypeId();
    }
}
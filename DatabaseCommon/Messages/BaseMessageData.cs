using System;

namespace Database.Common.Messages
{
    public abstract class BaseMessageData
    {
        public static BaseMessageData Decode(byte[] data)
        {
            int index = 0;
            int messageTypeId = ByteArrayHelper.ToInt32(data, ref index);
            MessageType messageTypeIdConverted = (MessageType)Enum.ToObject(typeof(MessageType), messageTypeId);
            switch (messageTypeIdConverted)
            {
                case MessageType.JoinAttempt:
                    return new JoinAttempt(data, index);

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